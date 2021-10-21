//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AzureAd.Icm.Types;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Prom2IcM
{
    /// <summary>
    /// This class convert Prometheus alert manager webhook to IcM alerts.
    /// IcM connector documentation:
    /// https://icmdocs.azurewebsites.net/developers/connectors-webhooks.html
    /// https://icmdocs.azurewebsites.net/developers/Connectors/InjectingIncidentsUsingConnectorAPI.html
    /// </summary>
    public class AlertRelay
    {
        private readonly ICMClientOptions _options;
        private readonly IICMClientProvider _icmClientProvider;
        private readonly Guid _connectorId;
        private readonly Serilog.ILogger _logger;

        public AlertRelay(IOptions<ICMClientOptions> options, IICMClientProvider icmClientProvider, Serilog.ILogger logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _icmClientProvider = icmClientProvider ?? throw new ArgumentNullException(nameof(icmClientProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _options.CheckValid();
            _connectorId = Guid.Parse(_options.IcmConnectorId);
            _logger.Information("ICMClientOptions: {ICMClientOptions}", _options);
        }

        public async Task GenerateIcMIncidentAsync(WebhookMessage webhookMessage)
        {
            if (webhookMessage == null)
            {
                throw new ArgumentNullException(nameof(webhookMessage));
            }

            if (webhookMessage.Alerts == null)
            {
                throw new InvalidOperationException($"'{nameof(webhookMessage.Alerts)}' are empty.");
            }

            using var ops = _logger.StartTimedOperation(nameof(GenerateIcMIncidentAsync));
            try
            {
                foreach (var alert in webhookMessage.Alerts)
                {
                    if (alert?.Status?.OrdinalEquals("firing") == true)
                    {
                        var icmIncident = await TransformPrometheusAlertToIcmIncidentAsync(webhookMessage, alert);
                        var icmClient = await _icmClientProvider.GetICMClientAsync();

                        var incidentSourceId = icmIncident.Source.IncidentId;

                        try
                        {
                            var existingIncidents = await icmClient.GetIncidentAlertSourceInfo2Async(_connectorId, new List<string> { incidentSourceId });
                            if (existingIncidents?.Any() == true)
                            {
                                _logger.Information(
                                    "Skip creating new incident, since there are already incidents with the same incidentSourceId '{incidentSourceId}'. IncidentIdList: {@IncidentIdList}",
                                    incidentSourceId,
                                    existingIncidents.Select(incident => incident.IncidentId));
                            }
                            else
                            {
                                var result = await icmClient.AddOrUpdateIncident2Async(_connectorId, icmIncident, RoutingOptions.None);
                                ProcessResult(result, icmIncident);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "Failed to submit incident to IcM");

                            if (IsTransientException(e))
                            {
                                _logger.Warning("Exception is transient and incident submit call should be retried");
                            }

                            // TODO: make sure AlertManager will retry the webhook if retuen is 500+.
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "failed_prom2icm.");
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        /// <summary>Processes the result</summary>
        /// <param name="result">incident submit result</param>
        /// <param name="source">submitted incident</param>
        private void ProcessResult(
            IncidentAddUpdateResult result,
            AlertSourceIncident source)
        {
            string fmt;
            string extra = string.Empty;

            switch (result.Status)
            {
                case IncidentAddUpdateStatus.AddedNew:
                    fmt = ((result.SubStatus & IncidentAddUpdateSubStatus.Suppressed) == 0)
                              ? "Added new incident {0} (source id: [{1}]) to IcM"
                              : "Added new suppressed incident {0} (source id: [{1}]) to IcM";
                    break;

                case IncidentAddUpdateStatus.Discarded:
                    fmt = ((result.SubStatus & IncidentAddUpdateSubStatus.Suppressed) == 0)
                              ? "Discarded new incident and updated hit count of existing incident {0} (source id: [{1}]) in IcM."
                              : "Discarded incident as suppressed (source id: [{1}]) in IcM";
                    break;

                // This is applicable to non-fire-and-forget connectors only as it requires a connector be expected to update existing
                //  incidents in IcM.  Fire-and-forget connetcors should consider this an error as they should never expect to update
                //  existing incidents.
                case IncidentAddUpdateStatus.UpdatedExisting:
                    extra = result.SubStatus.ToString();
                    fmt = ((result.SubStatus & IncidentAddUpdateSubStatus.Suppressed) == 0)
                              ? "Updated existing incident {0} (source id: [{1}]) in IcM. Substatus: {2}"
                              : "Updated existing suppressed incident {0} (source id: [{1}]) in IcM. Substatus: {2}";
                    break;

                // This is applicable to non-fire-and-forget connectors only as it requires a connector be expected to update existing
                //  incidents in IcM.  Fire-and-forget connetcors should consider this an error as they should never expect to update
                //  existing incidents.
                case IncidentAddUpdateStatus.AlertSourceUpdatesPending:
                    fmt = "Could not update incident {0} (source id: [{1}]) in IcM because source changes are pending";
                    break;

                // This is applicable to non-fire-and-forget connectors only as it requires a connector be expected to update existing
                //  incidents in IcM.  Fire-and-forget connetcors should consider this an error as they should never expect to update
                //  existing incidents.
                case IncidentAddUpdateStatus.DidNotChangeExisting:
                    fmt = "Change to incident {0} (source id: [{1}]) has already been applied or was older than the last change " +
                          "received from the connector";
                    break;

                // This is applicable to non-fire-and-forget connectors only as it requires a connector be expected to update existing
                //  incidents in IcM.  Fire-and-forget connetcors should consider this an error as they should never expect to update
                //  existing incidents.
                case IncidentAddUpdateStatus.UpdateToHoldingNotAllowed:
                    fmt = "Incident {0} (source id: [{1}]) cannot be changed from a non-holding state to a holding state";
                    break;

                default:
                    fmt = "Unknown status for attempint to add / update incident {0} (source id: [{1}])";
                    break;
            }

            _logger.Information(string.Format(CultureInfo.InvariantCulture, fmt, result.IncidentId, source.Source.IncidentId, extra));
        }

        private async Task<AlertSourceIncident> TransformPrometheusAlertToIcmIncidentAsync(WebhookMessage webhookMessage, Alert promAlert)
        {
            // Get information about the running Azure compute from the instance metadata service.
            var instanceMeta = await InstanceMetaHelper.GetMetaInfoAsync();
            var incident = IncidentMessageGenerator.GenerateIncidentFromPrometheusAlert(webhookMessage, promAlert, instanceMeta, _options, _logger);
            return incident;
        }

        /// <summary>determines if the specified exception thrown from an ICM server call is transient or not</summary>
        /// <param name="icmException">exception to check</param>
        /// <returns>true if the exception is transient, false otherwise</returns>
        /// <remarks>this is not a comprehensive set of exceptions that can be retrieved, but does address common issues</remarks>
        private static bool IsTransientException(
            Exception icmException)
        {
            const string CodeTooManyRequestsSoCallThrottled = "429";
            const string CodeNetworkProblem = "NETWORK";
            const string CodeTimeoutIssue = "TIMEOUT";

            const string MsgSqlTimeout = "SqlException:​ Timeout expired";
            const string MsgSqlDeadlock = "deadlock";

            FaultException<IcmFault> faultException;
            CompareInfo comparer = CultureInfo.InvariantCulture.CompareInfo;
            string code;

            if (icmException is WebException || icmException is TimeoutException)
            {
                return true;
            }

            faultException = icmException as FaultException<IcmFault>;
            code = faultException != null && faultException.Detail != null ? faultException.Detail.Code : null;

            if (string.IsNullOrWhiteSpace(code) == false &&
                (string.Equals(code, CodeNetworkProblem, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(code, CodeTimeoutIssue, StringComparison.OrdinalIgnoreCase) || string.Equals(
                     code,
                     CodeTooManyRequestsSoCallThrottled,
                     StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (comparer.IndexOf(icmException.Message, MsgSqlTimeout, CompareOptions.IgnoreCase) > 0 || comparer.IndexOf(
                    icmException.Message,
                    MsgSqlDeadlock,
                    CompareOptions.IgnoreCase) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
