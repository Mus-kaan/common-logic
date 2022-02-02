//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Eventhub.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Redis.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts.AzureMonitor;
using Microsoft.Rest.Azure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.Management.Fluent.Azure;
using TimeSpan = System.TimeSpan;

namespace Microsoft.Liftr.Fluent
{
    /// <summary>
    /// This is not thread safe, since IAzure is not thread safe by design.
    /// Please use 'LiftrAzureFactory' to dynamiclly generate it.
    /// Please do not add 'LiftrAzure' to the dependency injection container, use 'LiftrAzureFactory' instead.
    /// </summary>
    internal partial class LiftrAzure : ILiftrAzure
    {
        public const string c_AspEnv = "ASPNETCORE_ENVIRONMENT";
        private readonly LiftrAzureOptions _options;
        private readonly ILogger _logger;

        public LiftrAzure(
            string tenantId,
            string defaultSubscriptionId,
            string spnObjectId,
            TokenCredential tokenCredential,
            AzureCredentials credentials,
            IAzure fluentClient,
            IAuthenticated authenticated,
            LiftrAzureOptions options,
            ILogger logger)
        {
            TenantId = tenantId;
            DefaultSubscriptionId = defaultSubscriptionId;
            SPNObjectId = spnObjectId;
            TokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
            AzureCredentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            FluentClient = fluentClient ?? throw new ArgumentNullException(nameof(fluentClient));
            Authenticated = authenticated ?? throw new ArgumentNullException(nameof(authenticated));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string TenantId { get; }

        public string DefaultSubscriptionId { get; }

        public string SPNObjectId { get; }

        public Serilog.ILogger Logger => _logger;

        public IAzure FluentClient { get; }

        public IAuthenticated Authenticated { get; }

        public TokenCredential TokenCredential { get; }

        public AzureCredentials AzureCredentials { get; }

        public LiftrAzureOptions Options => _options;

        public async Task<string> GetResourceAsync(string resourceId, string apiVersion, CancellationToken cancellationToken = default)
        {
            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                if (string.IsNullOrEmpty(resourceId))
                {
                    throw new ArgumentNullException(nameof(resourceId));
                }

                if (string.IsNullOrEmpty(apiVersion))
                {
                    throw new ArgumentNullException(nameof(apiVersion));
                }

                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = resourceId;
                uriBuilder.Query = $"api-version={apiVersion}";
                _logger.Information($"Start getting resource at Uri: {uriBuilder.Uri}");
                var runOutputResponse = await _options.HttpPolicy.ExecuteAsync((ct) => httpClient.GetAsync(uriBuilder.Uri, ct), cancellationToken);

                if (runOutputResponse.StatusCode == HttpStatusCode.OK)
                {
                    return await runOutputResponse.Content.ReadAsStringAsync();
                }
                else if (runOutputResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    var errMsg = $"Failed at getting resource with Id '{resourceId}'. statusCode: '{runOutputResponse.StatusCode}'";
                    if (runOutputResponse?.Content != null)
                    {
                        errMsg = errMsg + $", response: {await runOutputResponse.Content?.ReadAsStringAsync()}";
                    }

                    var ex = new InvalidOperationException(errMsg);
                    _logger.Error(ex.Message);
                    throw ex;
                }
            }
        }

        public async Task PutResourceAsync(string resourceId, string apiVersion, string resourceJsonBody, CancellationToken cancellationToken = default)
        {
            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                if (string.IsNullOrEmpty(resourceId))
                {
                    throw new ArgumentNullException(nameof(resourceId));
                }

                if (string.IsNullOrEmpty(apiVersion))
                {
                    throw new ArgumentNullException(nameof(apiVersion));
                }

                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = resourceId;
                uriBuilder.Query = $"api-version={apiVersion}";

                _logger.Information($"Start putting resource at Uri: {uriBuilder.Uri}");
                using var httpContent = new StringContent(resourceJsonBody, Encoding.UTF8, "application/json");
                var runOutputResponse = await _options.HttpPolicy.ExecuteAsync((ct) => httpClient.PutAsync(uriBuilder.Uri, httpContent, ct), cancellationToken);

                if (!runOutputResponse.IsSuccessStatusCode)
                {
                    var errMsg = $"Failed at putting resource with Id '{resourceId}'. statusCode: '{runOutputResponse.StatusCode}'";
                    if (runOutputResponse?.Content != null)
                    {
                        errMsg = errMsg + $", response: {await runOutputResponse.Content?.ReadAsStringAsync()}";
                    }

                    var ex = new InvalidOperationException(errMsg);
                    _logger.Error(ex.Message);
                    throw ex;
                }
                else if (runOutputResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    await WaitAsyncOperationAsync(httpClient, runOutputResponse, cancellationToken);
                }

                _logger.Information($"Finished putting resource at Uri: {uriBuilder.Uri}");
            }
        }

        public async Task PatchResourceAsync(string resourceId, string apiVersion, string resourceJsonBody, CancellationToken cancellationToken = default)
        {
            using var handler = new AzureApiAuthHandler(AzureCredentials);
            using var httpClient = new HttpClient(handler);
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            if (string.IsNullOrEmpty(apiVersion))
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
            uriBuilder.Path = resourceId;
            uriBuilder.Query = $"api-version={apiVersion}";

            _logger.Information($"Start PATCH resource at Uri: {uriBuilder.Uri}");
            using var httpContent = new StringContent(resourceJsonBody, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(new HttpMethod("PATCH"), uriBuilder.Uri)
            {
                Content = httpContent,
            };
            var runOutputResponse = await _options.HttpPolicy.ExecuteAsync((ct) => httpClient.SendAsync(request, ct), cancellationToken);

            if (!runOutputResponse.IsSuccessStatusCode)
            {
                var errMsg = $"Failed at patching resource with Id '{resourceId}'. statusCode: '{runOutputResponse.StatusCode}'";
                if (runOutputResponse?.Content != null)
                {
                    errMsg = errMsg + $", response: {await runOutputResponse.Content?.ReadAsStringAsync()}";
                }

                var ex = new InvalidOperationException(errMsg);
                _logger.Error(ex.Message);
                throw ex;
            }
            else if (runOutputResponse.StatusCode == HttpStatusCode.Accepted)
            {
                await WaitAsyncOperationAsync(httpClient, runOutputResponse, cancellationToken);
            }

            _logger.Information($"Finished PATCH resource at Uri: {uriBuilder.Uri}");
        }

        public async Task DeleteResourceAsync(string resourceId, string apiVersion, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            if (string.IsNullOrEmpty(apiVersion))
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            // https://github.com/Azure/azure-rest-api-specs/blob/master/specification/imagebuilder/resource-manager/Microsoft.VirtualMachineImages/stable/2020-02-14/imagebuilder.json#L280
            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = resourceId;
                uriBuilder.Query = $"api-version={apiVersion}";
                _logger.Information($"Start deleting resource at Uri: {uriBuilder.Uri}");
                var deleteResponse = await _options.HttpPolicy.ExecuteAsync((ct) => httpClient.DeleteAsync(uriBuilder.Uri, ct), cancellationToken);
                _logger.Information($"DELETE response code: {deleteResponse.StatusCode}");

                if (!deleteResponse.IsSuccessStatusCode)
                {
                    _logger.Error($"Deleting resource at Uri: '{uriBuilder.Uri}' failed with error code '{deleteResponse.StatusCode}'");
                    if (deleteResponse?.Content != null)
                    {
                        var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                        _logger.Error("Error response body: {errorContent}", errorContent);
                    }

                    throw new InvalidOperationException($"Delete resource with id '{resourceId}' failed.");
                }
                else if (deleteResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    await WaitAsyncOperationAsync(httpClient, deleteResponse, cancellationToken);
                }

                _logger.Information($"Finished deleting resource at Uri: {uriBuilder.Uri}");
                return;
            }
        }

        #region Resource provider
        public Task<string> RegisterFeatureAsync(string resourceProviderName, string featureName)
        {
            return RegisterFeatureAsync(FluentClient.SubscriptionId, resourceProviderName, featureName);
        }

        public async Task<string> RegisterFeatureAsync(string subscriptionId, string resourceProviderName, string featureName)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            if (string.IsNullOrEmpty(resourceProviderName))
            {
                throw new ArgumentNullException(nameof(resourceProviderName));
            }

            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = $"/subscriptions/{subscriptionId}/providers/Microsoft.Features/providers/{resourceProviderName}/features/{featureName}/register";
                uriBuilder.Query = $"api-version=2015-12-01";

                _logger.Debug($"Start registering resource provider '{resourceProviderName}' in subscription '{subscriptionId}'");
                var response = await _options.HttpPolicy.ExecuteAsync(() => httpClient.PostAsync(uriBuilder.Uri, null));

                if (!response.IsSuccessStatusCode)
                {
                    var errMsg = $"Failed at registering resource provider. Status code: {response.Content}";

                    if (response.Content != null)
                    {
                        errMsg += $"Error content: {await response.Content.ReadAsStringAsync()}";
                    }

                    var ex = new InvalidOperationException(errMsg);
                    _logger.Error(ex, errMsg);
                    throw ex;
                }

                return await response.Content.ReadAsStringAsync();
            }
        }

        public Task<string> RegisterResourceProviderAsync(string resourceProviderName)
        {
            return RegisterResourceProviderAsync(FluentClient.SubscriptionId, resourceProviderName);
        }

        public async Task<string> RegisterResourceProviderAsync(string subscriptionId, string resourceProviderName)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            if (string.IsNullOrEmpty(resourceProviderName))
            {
                throw new ArgumentNullException(nameof(resourceProviderName));
            }

            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = $"/subscriptions/{subscriptionId}/providers/{resourceProviderName}/register";
                uriBuilder.Query = $"api-version=2014-04-01-preview";

                _logger.Information($"Start registering resource provider '{resourceProviderName}' in subscription '{subscriptionId}'");
                var response = await _options.HttpPolicy.ExecuteAsync(() => httpClient.PostAsync(uriBuilder.Uri, null));

                if (!response.IsSuccessStatusCode)
                {
                    var errMsg = $"Failed at registering resource provider. Status code: {response.Content}";

                    if (response.Content != null)
                    {
                        errMsg += $"Error content: {await response.Content.ReadAsStringAsync()}";
                    }

                    var ex = new InvalidOperationException(errMsg);
                    _logger.Error(ex, errMsg);
                    throw ex;
                }

                return await response.Content.ReadAsStringAsync();
            }
        }

        public Task<string> GetResourceProviderAsync(string resourceProviderName)
        {
            return GetResourceProviderAsync(FluentClient.SubscriptionId, resourceProviderName);
        }

        public async Task<string> GetResourceProviderAsync(string subscriptionId, string resourceProviderName)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            if (string.IsNullOrEmpty(resourceProviderName))
            {
                throw new ArgumentNullException(nameof(resourceProviderName));
            }

            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = $"/subscriptions/{subscriptionId}/providers/{resourceProviderName}";
                uriBuilder.Query = $"api-version=2014-04-01-preview";

                _logger.Information($"Start getting resource provider '{resourceProviderName}' in subscription '{subscriptionId}'");
                var response = await _options.HttpPolicy.ExecuteAsync(() => httpClient.GetAsync(uriBuilder.Uri));

                if (!response.IsSuccessStatusCode)
                {
                    var errMsg = $"Failed at getting resource provider. Status code: {response.Content}";

                    if (response.Content != null)
                    {
                        errMsg += $"Error content: {await response.Content.ReadAsStringAsync()}";
                    }

                    var ex = new InvalidOperationException(errMsg);
                    _logger.Error(ex, errMsg);
                    throw ex;
                }

                return await response.Content.ReadAsStringAsync();
            }
        }
        #endregion

        #region Identity
        public async Task<IIdentity> GetOrCreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags)
        {
            var msi = await GetMSIAsync(rgName, msiName);
            if (msi == null)
            {
                msi = await CreateMSIAsync(location, rgName, msiName, tags);
            }

            return msi;
        }

        public async Task<IIdentity> CreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags)
        {
            _logger.Information("Creating a Managed Identity with name {msiName} ...", msiName);
            using var ops = _logger.StartTimedOperation(nameof(CreateMSIAsync));
            try
            {
                var msi = await FluentClient.Identities
                    .Define(msiName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(rgName)
                    .WithTags(tags)
                    .CreateAsync();

                _logger.Information("Created Managed Identity with Id {ResourceId} ...", msi.Id);
                return msi;
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public Task<IIdentity> GetMSIAsync(string rgName, string msiName)
        {
            _logger.Information("Getting Managed Identity with name {msiName} in RG {rgName} ...", msiName, rgName);
            return FluentClient.Identities.GetByResourceGroupAsync(rgName, msiName);
        }
        #endregion

        #region ACR
        public async Task<IRegistry> GetOrCreateACRAsync(Region location, string rgName, string acrName, IDictionary<string, string> tags)
        {
            var acr = await GetACRAsync(rgName, acrName);

            if (acr == null)
            {
                var helper = new ACRHelper(_logger);
                acr = await helper.CreateACRAsync(this, location, rgName, acrName, tags);
                _logger.Information("Created ACR with Id {resourceId}.", acr.Id);
            }

            await ConfigureImageScanningAsync(acr);

            return acr;
        }

        public async Task ConfigureImageScanningAsync(IRegistry acr)
        {
            var eventGridHelper = new EventGridHelper(_logger);
            var scope = "Microsoft.ContainerRegistry/registries/" + acr.Name;
            bool imageScanConfigured = await eventGridHelper.IsEventSubscriptionExistingAsync(this, scope);
            if (!imageScanConfigured)
            {
                await eventGridHelper.CreateImageScanningEventSubscriptionForACRAsync(this, acr);
                _logger.Information("Created Event Subscription for Image Scanning");
            }
        }

        public Task<IRegistry> GetACRAsync(string rgName, string acrName)
        {
            _logger.Information("Getting the ACR {acrName} in RG {rgName} ...", acrName, rgName);
            return FluentClient.ContainerRegistries.GetByResourceGroupAsync(rgName, acrName);
        }

        public async Task<IEnumerable<IRegistry>> ListACRAsync(string rgName)
        {
            _logger.Information("Listing all ACR in RG {rgName} ...", rgName);
            var list = await FluentClient.ContainerRegistries.ListByResourceGroupAsync(rgName);
            return list?.ToList();
        }
        #endregion

        #region Deployments
        public async Task<IDeployment> CreateDeploymentAsync(Region location, string rgName, string template, string templateParameters = null, bool noLogging = false, CancellationToken cancellationToken = default)
        {
            var deploymentName = SdkContext.RandomResourceName("LiftrFluentSDK", 24);
            if (string.IsNullOrEmpty(template))
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (string.IsNullOrEmpty(templateParameters))
            {
                templateParameters = "{}";
            }

            using (var ops = _logger.StartTimedOperation("ARMTemplateDeployment"))
            {
                ops.SetContextProperty("DeploymentSubscriptionId", FluentClient.SubscriptionId);
                ops.SetContextProperty("DeploymentResourceGroup", rgName);

                _logger.Information($"Starting an incremental ARM deployment with name {deploymentName} ...");
                if (!noLogging)
                {
                    _logger.Information("Deployment template: {@template}", template);
                    _logger.Information("Deployment template Parameters: {@templateParameters}", templateParameters);
                }

                try
                {
                    var deployment = await FluentClient.Deployments
                        .Define(deploymentName)
                        .WithExistingResourceGroup(rgName)
                        .WithTemplate(template)
                        .WithParameters(templateParameters)
                        .WithMode(DeploymentMode.Incremental)
                        .CreateAsync(cancellationToken);

                    _logger.Information($"Finished the ARM deployment with name {deploymentName} ...");
                    return deployment;
                }
                catch (Exception ex)
                {
                    ops.FailOperation("ARM deployment failed");

                    try
                    {
                        var error = await DeploymentExtensions.GetDeploymentErrorDetailsAsync(FluentClient.SubscriptionId, rgName, deploymentName, AzureCredentials);
                        _logger.Error(
                            ex,
                            "Failed ARM deployment with name {deploymentName} (in resource group '{rgName}' of subscription '{subscriptionId}'). Error: {@DeploymentError}",
                            deploymentName,
                            rgName,
                            FluentClient.SubscriptionId,
                            error);

                        throw new ARMDeploymentFailureException("ARM deployment failed", ex) { Details = error };
                    }
                    catch
                    {
                        _logger.Error(
                            ex,
                            "Failed ARM deployment with name {deploymentName} (in resource group '{rgName}' of subscription '{subscriptionId}').",
                            deploymentName,
                            rgName,
                            FluentClient.SubscriptionId);

                        if (ex is CloudException)
                        {
                            var cloudEx = ex as CloudException;
                            _logger.Error("Failure details: " + cloudEx.Response.Content);
                        }

                        throw new ARMDeploymentFailureException("ARM deployment failed", ex);
                    }
                }
            }
        }
        #endregion

        #region Monitoring
        public async Task<string> GetOrCreateLogAnalyticsWorkspaceAsync(Region location, string rgName, string name, IDictionary<string, string> tags)
        {
            var logAnalytics = await GetLogAnalyticsWorkspaceAsync(rgName, name);
            if (logAnalytics == null)
            {
                var helper = new LogAnalyticsHelper(_logger);
                await helper.CreateLogAnalyticsWorkspaceAsync(this, location, rgName, name, tags);
                logAnalytics = await GetLogAnalyticsWorkspaceAsync(rgName, name);
                _logger.Information("Created a new Log Analytics Workspace");
            }

            return logAnalytics;
        }

        public Task<string> GetLogAnalyticsWorkspaceAsync(string rgName, string name)
        {
            var helper = new LogAnalyticsHelper(_logger);
            return helper.GetLogAnalyticsWorkspaceAsync(this, rgName, name);
        }

        public async Task<IActionGroup> GetOrUpdateActionGroupAsync(string rgName, string name, string receiverName, string email)
        {
            _logger.Information("Getting Action Group. rgName: {rgName}, name: {name} ...", rgName, name);
            IActionGroup ag;
            try
            {
                var subId = FluentClient.GetCurrentSubscription();
                ag = await FluentClient
                    .ActionGroups.GetByIdAsync($"/subscriptions/{subId}/resourceGroups/{rgName}/providers/microsoft.insights/actionGroups/{name}");
            }
            catch (Azure.Management.Monitor.Fluent.Models.ErrorResponseException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Information("Creating a Action Group. rgName: {rgName}, name: {name} ...", rgName, name);
                ag = await FluentClient.ActionGroups.Define(name)
                    .WithExistingResourceGroup(rgName)
                    .DefineReceiver(receiverName)
                    .WithEmail(email)
                    .Attach()
                    .CreateAsync();
            }

            return ag;
        }

        public async Task<IMetricAlert> GetOrUpdateMetricAlertAsync(string rgName, MetricAlertOptions alertOptions)
        {
            _logger.Information("Getting Metric Alert. rgName: {rgName}, name: {name} ...", rgName, alertOptions.Name);
            IMetricAlert ma;
            try
            {
                var subId = FluentClient.GetCurrentSubscription();
                ma = await FluentClient
                    .AlertRules.MetricAlerts.GetByIdAsync($"/subscriptions/{subId}/resourceGroups/{rgName}/providers/microsoft.insights/scheduledqueryrules/{alertOptions.Name}");
            }
            catch (Azure.Management.Monitor.Fluent.Models.ErrorResponseException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Information("Creating a Metric Alert. rgName: {rgName}, name: {name} ...", rgName, alertOptions.Name);
                ma = await FluentClient.AlertRules.MetricAlerts.Define(alertOptions.Name)
                    .WithExistingResourceGroup(rgName)
                    .WithTargetResource(alertOptions.TargetResourceId)
                    .WithPeriod(TimeSpan.FromMinutes(alertOptions.AggregationPeriod))
                    .WithFrequency(TimeSpan.FromMinutes(alertOptions.FrequencyOfEvaluation))
                    .WithAlertDetails(alertOptions.Severity, alertOptions.Description)
                    .WithActionGroups(alertOptions.ActionGroupResourceId)
                    .DefineAlertCriteria(alertOptions.AlertConditionName)
                    .WithMetricName(alertOptions.MetricName, alertOptions.MetricNamespace)
                    .WithCondition(MetricAlertRuleTimeAggregation.Parse(alertOptions.TimeAggregationType), MetricAlertRuleCondition.Parse(alertOptions.ConditionOperator), alertOptions.Threshold)
                    .Attach()
                    .CreateAsync();
            }

            return ma;
        }
        #endregion

        #region Event Hub
        public async Task<IEventHubNamespace> GetOrCreateEventHubNamespaceAsync(Region location, string rgName, string name, int throughtputUnits, int maxThroughtputUnits, IDictionary<string, string> tags)
        {
            _logger.Information("Getting Event hub namespace. rgName: {rgName}, name: {name} ...", rgName, name);
            IEventHubNamespace eventHubNamespace = null;
            try
            {
                eventHubNamespace = await FluentClient
                    .EventHubNamespaces
                    .GetByResourceGroupAsync(rgName, name);
            }
            catch (Azure.Management.EventHub.Fluent.Models.ErrorResponseException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Information("Creating a Event hub namespace. rgName: {rgName}, name: {name} ...", rgName, name);
                eventHubNamespace = await FluentClient
                    .EventHubNamespaces
                    .Define(name)
                    .WithRegion(location)
                    .WithExistingResourceGroup(rgName)
                    .WithAutoScaling()
                    .WithCurrentThroughputUnits(throughtputUnits)
                    .WithThroughputUnitsUpperLimit(maxThroughtputUnits)
                    .WithTags(tags)
                    .CreateAsync();
            }

            return eventHubNamespace;
        }

        public async Task<IEventHub> GetOrCreateEventHubAsync(Region location, string rgName, string namespaceName, string hubName, int partitionCount, int throughtputUnits, int maxThroughtputUnits, IList<string> consumerGroups, IDictionary<string, string> tags)
        {
            _logger.Information("Getting Event Hub. rgName: {rgName}, namespaceName: {namespaceName}, hubName: {hubName} ...", rgName, namespaceName, hubName);
            IEventHub eventhub = null;

            if (consumerGroups == null)
            {
                throw new ArgumentNullException(nameof(consumerGroups));
            }

            try
            {
                eventhub = await FluentClient
                    .EventHubs
                    .GetByNameAsync(rgName, namespaceName, hubName);
            }
            catch (Azure.Management.EventHub.Fluent.Models.ErrorResponseException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Information("Cannot find Event Hub. rgName: {rgName}, namespaceName: {namespaceName}, hubName: {hubName} ...", rgName, namespaceName, hubName);
                IEventHubNamespace eventHubNamespace = await GetOrCreateEventHubNamespaceAsync(location, rgName, namespaceName, throughtputUnits, maxThroughtputUnits, tags);

                _logger.Information($"Creating a Event Hub with namespaceName {namespaceName}, name {hubName} ...", namespaceName, hubName);

                var eventHubBuilder = FluentClient
                    .EventHubs
                    .Define(hubName)
                    .WithExistingNamespace(eventHubNamespace)
                    .WithPartitionCount(partitionCount);

                foreach (var consumerGroup in consumerGroups)
                {
                    eventHubBuilder.WithNewConsumerGroup(consumerGroup);
                }

                eventhub = await eventHubBuilder.CreateAsync();
            }

            return eventhub;
        }
        #endregion

        #region Shared Image Gallery
        public async Task<IGalleryImageVersion> GetImageVersionAsync(
            string rgName,
            string galleryName,
            string imageName,
            string imageVersionName)
        {
            if (imageName == null)
            {
                throw new ArgumentNullException(nameof(imageName));
            }

            _logger.Information("Getting image verion. imageVersionName:{imageVersionName}, galleryName: {galleryName}, imageName: {imageName}", imageVersionName, galleryName, imageName);

            try
            {
                var galleryImageVersion = await FluentClient.GalleryImageVersions
                .GetByGalleryImageAsync(rgName, galleryName, imageName, imageVersionName);
                return galleryImageVersion;
            }
            catch (CloudException ex) when (ex.IsNotFound())
            {
                return null;
            }
        }
        #endregion

        public async Task<string> WaitAsyncOperationAsync(
           HttpClient httpClient,
           HttpResponseMessage startOperationResponse,
           CancellationToken cancellationToken,
           TimeSpan? pollingTime = null)
        {
            string statusUrl = string.Empty;

            if (startOperationResponse.Headers.Contains("Location"))
            {
                statusUrl = startOperationResponse.Headers.GetValues("Location").FirstOrDefault();
            }

            if (string.IsNullOrEmpty(statusUrl) && startOperationResponse.Headers.Contains("Azure-AsyncOperation"))
            {
                statusUrl = startOperationResponse.Headers.GetValues("Azure-AsyncOperation").FirstOrDefault();
            }

            if (string.IsNullOrEmpty(statusUrl))
            {
                var ex = new InvalidOperationException("Cannot find the async status url from both the headers: Location, AsyncOperation");
                _logger.LogError(ex.Message);
                throw ex;
            }

            while (true)
            {
                var statusResponse = await _options.HttpPolicy.ExecuteAsync((ct) => httpClient.GetAsync(new Uri(statusUrl), ct), cancellationToken);
                var body = await statusResponse.Content.ReadAsStringAsync();
                bool keepWaiting = false;

                if (body.OrdinalContains("Running") ||
                    body.OrdinalContains("InProgress"))
                {
                    keepWaiting = true;
                }
                else if (body.OrdinalContains("Succeeded") ||
                    body.OrdinalContains("Failed") ||
                    body.OrdinalContains("Canceled") ||
                    (statusResponse.StatusCode != HttpStatusCode.Accepted && statusResponse.StatusCode != HttpStatusCode.Created))
                {
                    keepWaiting = false;
                }
                else
                {
                    keepWaiting = true;
                }

                if (keepWaiting)
                {
                    var retryAfter = pollingTime.HasValue ? pollingTime.Value : GetRetryAfterValue(statusResponse);
                    _logger.Information($"Wait for {retryAfter.TotalSeconds} seconds before checking the async status at: '{statusUrl}'");
                    await Task.Delay(retryAfter, cancellationToken);
                }
                else
                {
                    return body;
                }
            }
        }

        public bool IsAMETenant()
        {
            return TenantId.OrdinalEquals("33e01921-4d64-4f8c-a055-5bdaffd5e33d");
        }

        public bool IsMicrosoftTenant()
        {
            return TenantId.OrdinalEquals("72f988bf-86f1-41af-91ab-2d7cd011db47");
        }

        private TimeSpan GetRetryAfterValue(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta;
            if (retryAfter == null)
            {
                return TimeSpan.FromSeconds(10);
            }

            return retryAfter.Value;
        }
    }
}
