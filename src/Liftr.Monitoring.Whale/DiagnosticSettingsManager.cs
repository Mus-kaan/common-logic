//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.EventHubManager;
using Microsoft.Liftr.Hosting.Swagger;
using Microsoft.Liftr.Monitoring.Common;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Whale
{
    public class DiagnosticSettingsManager
    {
        private readonly IAzureClientsProvider _clientProvider;
        private readonly IEventHubManager _eventHubManager;
        private readonly ILogger _logger;

        public DiagnosticSettingsManager(
            IAzureClientsProvider clientProvider,
            IEventHubManager eventHubManager,
            ILogger logger)
        {
            _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
            _eventHubManager = eventHubManager ?? throw new ArgumentNullException(nameof(eventHubManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Properties

        /// <summary>
        /// True if the operation succeeded, false otherwise.
        /// </summary>
        public bool SuccessfulOperation { get; internal set; } = true;

        /// <summary>
        /// The reason for the operation status, in case of adding a diagnostic setting.
        /// </summary>
        public MonitoringStatusReason Reason { get; internal set; } = MonitoringStatusReason.CapturedByRules;

        /// <summary>
        /// The name of the event hub associated to the operation.
        /// </summary>
        public string EventHubName { get; internal set; } = string.Empty;

        /// <summary>
        /// The id of the authorization rule associated to the operation.
        /// </summary>
        public string AuthorizationRuleId { get; internal set; } = string.Empty;

        /// <summary>
        /// The name of the diagnostic setting associated to the operation.
        /// </summary>
        public string DiagnosticSettingName { get; internal set; } = string.Empty;

        #endregion

        #region Public methods

        public async Task AddDiagnosticSettingToResourceAsync(MonitoredResource resource, string monitorId, string tenantId)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            try
            {
                TryGetEventHubEntity(resource, monitorId);
                if (!Reason.ActiveMonitoringStateForResource())
                {
                    SuccessfulOperation = false;
                    return;
                }

                var fluentClient = await GetFluentClientAsync(monitorId, tenantId);
                var logsCategories = await GetLogsCategoriesForResourceAsync(fluentClient, resource.Id);

                if (!logsCategories.Any())
                {
                    // Resource type does not support any log categories; nothing to do here
                    _logger.Information("Resource {resourceId} does not support log categories for diagnostic settings.", resource.Id);

                    SuccessfulOperation = false;
                    Reason = MonitoringStatusReason.ResourceTypeNotSupported;

                    return;
                }

                var existingDiagnosticSettings = await fluentClient.DiagnosticSettings
                    .ListByResourceAsync(resource.Id);

                var sameSinkDiagnosticSettings = existingDiagnosticSettings.Where(ds => ds.EventHubAuthorizationRuleId.OrdinalEquals(AuthorizationRuleId));

                if (sameSinkDiagnosticSettings.Any())
                {
                    // Resource already has diagnostic setting with this authorization rule
                    // Re-adjust the diagnostic setting name to avoid conflicts
                    DiagnosticSettingName = sameSinkDiagnosticSettings.First().Name;
                }
                else if (existingDiagnosticSettings.Count() >= 5)
                {
                    // Resource has 5 diagnostic settings, and reached the maximum limit; we cannot proceed
                    _logger.Information(
                        "Resource {resourceId} can't be monitored as it already has 5 diagnostic settings.",
                        resource.Id);

                    SuccessfulOperation = false;
                    Reason = MonitoringStatusReason.DiagnosticSettingsLimitReached;

                    return;
                }
                else
                {
                    // We need to create a new diagnostic setting on the resource
                    DiagnosticSettingName = MonitorTagsAndDiagnosticUtils.GetDiagnosticSettingNameForResource();
                }

                _logger.Information(
                    "Creating diagnostic setting {diagnosticSettingName} for resource {resourceId} and monitor {monitorId}.",
                    DiagnosticSettingName,
                    resource.Id,
                    monitorId);

                await fluentClient.DiagnosticSettings
                    .Define(DiagnosticSettingName)
                    .WithResource(resource.Id)
                    .WithEventHub(AuthorizationRuleId, EventHubName)
                    .WithLogsAndMetrics(logsCategories, TimeSpan.MaxValue, 1)
                    .CreateAsync();

                _logger.Information(
                    "Created diagnostic setting {diagnosticSettingName} for resource {resourceId} and monitor {monitorId} successfully.",
                    DiagnosticSettingName,
                    resource.Id,
                    monitorId);

                SuccessfulOperation = true;
            }
            catch (ErrorResponseException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.Information(
                    "Received conflict response when trying to monitor resource {resourceId} for monitor {monitorId}. Swallowing exception {ex}",
                    resource.Id,
                    monitorId,
                    ex.ToJson());

                Reason = MonitoringStatusReason.ConflictStatus;

                try
                {
                    // TODO- should check the default error response class difference
                    var error = ex.Response.Content.FromJson<ResourceProviderDefaultErrorResponse>();
                    if (error.Error.Code.OrdinalEquals("ScopeLocked"))
                    {
                        Reason = MonitoringStatusReason.ScopeLocked;
                    }
                }
                catch
                {
                    // If failed to de-serialize, don't do anything and proceed with ConflictStatus reason
                }

                SuccessfulOperation = false;
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to add diagnostic setting to resource {resourceId} with monitor {monitorId}. Exception {ex}",
                    resource.Id,
                    monitorId,
                    ex.ToJson());

                Reason = MonitoringStatusReason.Other;
                SuccessfulOperation = false;
            }
        }

        public async Task RemoveDiagnosticSettingFromResourceAsync(
            MonitoredResource resource,
            string diagnosticSettingName,
            string monitorId,
            string tenantId)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            try
            {
                var fluentClient = await GetFluentClientAsync(monitorId, tenantId);

                _logger.Information(
                    "Deleting diagnostic setting {diagnosticSettingName} from resource {resourceId} and monitor {monitorId}.",
                    diagnosticSettingName,
                    resource.Id,
                    monitorId);

                var allDiagnosticSettings = await fluentClient.DiagnosticSettings
                    .ListByResourceAsync(resource.Id);

                if (allDiagnosticSettings.Any(ds => ds.Name == diagnosticSettingName))
                {
                    await fluentClient.DiagnosticSettings
                        .DeleteAsync(resource.Id, diagnosticSettingName);

                    _logger.Information(
                        "Deleted diagnostic setting {diagnosticSettingName} from resource {resourceId} and monitor {monitorId} successfully.",
                        diagnosticSettingName,
                        resource.Id,
                        monitorId);
                }
                else
                {
                    _logger.Information(
                        "Could not find diagnostic setting {diagnosticSettingName} at resource {resourceId} and monitor {monitorId}. Skipping deletion.",
                        diagnosticSettingName,
                        resource.Id,
                        monitorId);
                }

                SuccessfulOperation = true;
            }
            catch (ErrorResponseException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.Information(
                    "Could not list diagnostic settings for resource {resourceId} and monitor {monitorId}. Resource might have been deleted.",
                    resource.Id,
                    monitorId);

                SuccessfulOperation = true;
            }
            catch (ErrorResponseException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.Information(
                    "Received conflict response when trying to stop monitoring resource {resourceId} for monitor {monitorId}. Checking if exception {ex} is related to locked scopes",
                    resource.Id,
                    monitorId,
                    ex.ToJson());

                ResourceProviderDefaultErrorResponse error = null;

                try
                {
                    error = ex.Response.Content.FromJson<ResourceProviderDefaultErrorResponse>();
                }
                catch
                {
                    // If failed to de-serialize, re-throw
                    throw;
                }

                if (error.Error.Code.OrdinalEquals("ScopeLocked"))
                {
                    _logger.Information(
                        "Resource {resourceId} is locked. Stopping monitoring it with monitor {monitorId}.", resource.Id, monitorId);

                    // Since scope is locked, we consider a successful deletion and delete entity from DB
                    SuccessfulOperation = true;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to delete diagnostic setting {diagnosticSettingName} from resource {resourceId} and monitor {monitorId}. Exception {ex}",
                    diagnosticSettingName,
                    resource.Id,
                    monitorId,
                    ex.ToJson());

                SuccessfulOperation = false;
            }
        }

        public async Task AddDiagnosticSettingToSubscriptionAsync(
            MonitoredResource subscriptionMonitoredResource,
            string subscriptionId,
            string monitorId,
            string tenantId)
        {
            if (subscriptionMonitoredResource == null)
            {
                throw new ArgumentNullException(nameof(subscriptionMonitoredResource));
            }

            try
            {
                TryGetEventHubEntity(subscriptionMonitoredResource, monitorId);
                if (!Reason.ActiveMonitoringStateForResource())
                {
                    SuccessfulOperation = false;
                    return;
                }

                var fluentClient = await GetFluentClientAsync(monitorId, tenantId);

                var existingDiagnosticSettings = await fluentClient.DiagnosticSettings.Manager.Inner.SubscriptionDiagnosticSettings
                    .ListAsync(subscriptionId);

                var sameSinkDiagnosticSettings = existingDiagnosticSettings.Value.Where(ds => ds.EventHubAuthorizationRuleId.OrdinalEquals(AuthorizationRuleId));

                if (sameSinkDiagnosticSettings.Any())
                {
                    // Subscription already has diagnostic setting with this authorization rule
                    // Re-adjust the diagnostic setting name to avoid conflicts
                    DiagnosticSettingName = sameSinkDiagnosticSettings.First().Name;
                }
                else if (existingDiagnosticSettings.Value.Count() >= MonitorTagsAndDiagnosticUtils.MaxDiagnosticSettings)
                {
                    // subscription has 5 diagnostic settings, and reached the maximum limit; we cannot proceed
                    _logger.Information(
                        "Subscription {subscriptionId} can't be monitored as it already has 5 diagnostic settings.",
                        subscriptionId);

                    SuccessfulOperation = false;
                    Reason = MonitoringStatusReason.DiagnosticSettingsLimitReached;

                    return;
                }
                else
                {
                    // We need to create a new diagnostic setting on the subscription
                    DiagnosticSettingName = MonitorTagsAndDiagnosticUtils.GetDiagnosticSettingNameForResource();
                }

                var logsCategories = await GetLogsCategoriesForSubscriptionAsync(fluentClient);

                _logger.Information(
                    "Creating diagnostic setting {diagnosticSettingName} for subscription {subscriptionId} and monitor {monitorId}.",
                    DiagnosticSettingName,
                    subscriptionId,
                    monitorId);

                var diagnosticSettingParameters = new SubscriptionDiagnosticSettingsResourceInner()
                {
                    EventHubName = EventHubName,
                    EventHubAuthorizationRuleId = AuthorizationRuleId,
                    Logs = logsCategories.ToList(),
                };

                await fluentClient.DiagnosticSettings.Manager.Inner.SubscriptionDiagnosticSettings
                    .CreateOrUpdateAsync(subscriptionId, diagnosticSettingParameters, DiagnosticSettingName);

                _logger.Information(
                    "Created diagnostic setting {diagnosticSettingName} for subscription {subscriptionId} and monitor {monitorId} successfully.",
                    DiagnosticSettingName,
                    subscriptionId,
                    monitorId);

                SuccessfulOperation = true;
            }
            catch (ErrorResponseException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.Warning(
                    "Received conflict response when trying to monitor subscription {subscriptionId} for monitor {monitorId}. Swallowing exception {ex}",
                    subscriptionMonitoredResource.Id,
                    monitorId,
                    ex.ToJson());

                Reason = MonitoringStatusReason.ConflictStatus;
                SuccessfulOperation = false;
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to add diagnostic setting to subscription {subscriptionId} with monitor {monitorId}. Exception {ex}",
                    subscriptionMonitoredResource.Id,
                    monitorId,
                    ex.ToJson());

                Reason = MonitoringStatusReason.Other;
                SuccessfulOperation = false;
            }
        }

        public async Task RemoveDiagnosticSettingFromSubscriptionAsync(
            string subscriptionId,
            string diagnosticSettingName,
            string monitorId,
            string tenantId)
        {
            var fluentClient = await GetFluentClientAsync(monitorId, tenantId);

            _logger.Information(
                "Deleting diagnostic setting {diagnosticSettingName} from subscription {subscriptionId} and monitor {monitorId}.",
                diagnosticSettingName,
                subscriptionId,
                monitorId);

            try
            {
                var allDiagnosticSettings = await fluentClient.DiagnosticSettings.Manager.Inner.SubscriptionDiagnosticSettings
                    .ListAsync(subscriptionId);

                if (allDiagnosticSettings.Value.Any(ds => ds.Name == diagnosticSettingName))
                {
                    await fluentClient.DiagnosticSettings.Manager.Inner.SubscriptionDiagnosticSettings
                        .DeleteAsync(subscriptionId, diagnosticSettingName);

                    _logger.Information(
                        "Deleted diagnostic setting {diagnosticSettingName} from subscription {subscriptionId} and monitor {monitorId} successfully.",
                        diagnosticSettingName,
                        subscriptionId,
                        monitorId);
                }
                else
                {
                    _logger.Information(
                        "Could not find diagnostic setting {diagnosticSettingName} at subscription {subscriptionId} and monitor {monitorId}. Skipping deletion.",
                        diagnosticSettingName,
                        subscriptionId,
                        monitorId);
                }

                SuccessfulOperation = true;
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to delete diagnostic setting {diagnosticSettingName} from subscription {subscriptionId} and monitor {monitorId}. Exception {ex}",
                    diagnosticSettingName,
                    subscriptionId,
                    monitorId,
                    ex);

                SuccessfulOperation = false;
            }
        }

        #endregion

        #region Private methods

        private void TryGetEventHubEntity(MonitoredResource resource, string monitorId)
        {
            _logger.Information(
                "Obtaining event hub metadata for resource {resourceId} at location {location} for monitor {monitorId}.",
                resource.Id,
                resource.Location,
                monitorId);

            var eventHubEntity = _eventHubManager.Get(resource.Location);

            if (eventHubEntity == null)
            {
                _logger.Information(
                    "Could not find an event hub for resource {resourceId} and monitor {monitorId}. Location {location} is not supported.",
                    resource.Id,
                    monitorId,
                    resource.Location);

                Reason = MonitoringStatusReason.LocationNotSupported;
            }
            else
            {
                EventHubName = eventHubEntity.Name;
                AuthorizationRuleId = eventHubEntity.AuthorizationRuleId;

                _logger.Information(
                    "Obtained event hub {eventHubName} and authorization rule {authorizationRuleId} for resource {resourceId} and monitor {monitorId} successfully.",
                    EventHubName,
                    AuthorizationRuleId,
                    resource.Id,
                    monitorId);
            }
        }

        private static async Task<IEnumerable<IDiagnosticSettingsCategory>> GetLogsCategoriesForResourceAsync(
            IAzure fluentClient, string resourceId)
        {
            var categories = await fluentClient.DiagnosticSettings
                        .ListCategoriesByResourceAsync(resourceId) ?? new List<IDiagnosticSettingsCategory>();

            var logsCategories = categories.Where(c => c.Type == CategoryType.Logs);

            return logsCategories;
        }

        private static async Task<IEnumerable<SubscriptionLogSettings>> GetLogsCategoriesForSubscriptionAsync(
            IAzure fluentClient)
        {
            var categories = await fluentClient.ActivityLogs
                .ListEventCategoriesAsync();

            var logsCategories = categories.Select(c => new SubscriptionLogSettings()
            {
                Category = c.Value,
                Enabled = true,
            });

            return logsCategories;
        }

        private async Task<IAzure> GetFluentClientAsync(string monitorId, string tenantId)
        {
            var parsedMonitorId = new ResourceId(monitorId);
            var fluentClient = await _clientProvider.GetFluentClientAsync(parsedMonitorId.SubscriptionId, tenantId);
            return fluentClient;
        }

        #endregion
    }
}
