//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AzureAd.Icm.Types;
using Microsoft.Liftr.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Microsoft.Liftr.IcmConnector
{
    /// <summary>
    /// This class convert Prometheus alert manager webhook to IcM alerts.
    /// IcM connector documentation:
    /// https://icmdocs.azurewebsites.net/developers/connectors-webhooks.html
    /// https://icmdocs.azurewebsites.net/developers/Connectors/InjectingIncidentsUsingConnectorAPI.html
    /// </summary>
    public class AlertRelay
    {
        private readonly IICMClientProvider _icmClientProvider;
        private readonly Serilog.ILogger _logger;

        public AlertRelay(IICMClientProvider icmClientProvider, Serilog.ILogger logger)
        {
            _icmClientProvider = icmClientProvider ?? throw new ArgumentNullException(nameof(icmClientProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task GenerateIcMFromPrometheusAsync(PrometheusWebhookMessage webhookMessage)
        {
            if (webhookMessage == null)
            {
                throw new ArgumentNullException(nameof(webhookMessage));
            }

            if (webhookMessage.Alerts == null)
            {
                throw new InvalidOperationException($"'{nameof(webhookMessage.Alerts)}' are empty.");
            }

            using var ops = _logger.StartTimedOperation(nameof(GenerateIcMFromGrafanaAsync));
            try
            {
                var icmClient = await _icmClientProvider.GetICMClientAsync();
                if (icmClient == null)
                {
                    _logger.Warning("Cannot get IcM client. Probably IcM is not enabled.");
                    return;
                }

                var icmClientOptions = _icmClientProvider.GetClientOptions();
                var connectorId = Guid.Parse(icmClientOptions.IcmConnectorId);

                foreach (var alert in webhookMessage.Alerts)
                {
                    if (!ShouldProcessPrometheusAlert(alert))
                    {
                        continue;
                    }

                    bool isFiring = false;
                    if (alert?.Status?.OrdinalEquals("firing") == true)
                    {
                        isFiring = true;
                    }
                    else if (alert?.Status?.OrdinalEquals("resolved") == true && !string.IsNullOrEmpty(alert.EndsAt))
                    {
                        isFiring = false;
                    }
                    else
                    {
                        continue;
                    }

                    var icmIncident = await TransformPrometheusAlertToIcmIncidentAsync(webhookMessage, alert);

                    var incidentSourceId = icmIncident.Source.IncidentId;

                    try
                    {
                        var existingIncidents = await icmClient.GetIncidentAlertSourceInfo2Async(connectorId, new List<string> { incidentSourceId });
                        if (isFiring)
                        {
                            // alert firing.
                            if (existingIncidents?.Any() == true)
                            {
                                _logger.Information(
                                    "Skip creating new incident, since there are already incidents with the same incidentSourceId '{incidentSourceId}'. IncidentIdList: {@IncidentIdList}",
                                    incidentSourceId,
                                    existingIncidents.Select(incident => incident.IncidentId));
                            }
                            else
                            {
                                var result = await icmClient.AddOrUpdateIncident2Async(connectorId, icmIncident, RoutingOptions.None);
                                ProcessResult(result, icmIncident);
                            }
                        }
                        else
                        {
                            // alert resolved.
                            if (existingIncidents?.Any() != true)
                            {
                                _logger.Information(
                                    "Skip mitigating incident, since there are no incidents with the same incidentSourceId '{incidentSourceId}'. IncidentIdList: {@IncidentIdList}",
                                    incidentSourceId,
                                    existingIncidents.Select(incident => incident.IncidentId));
                            }
                            else
                            {
                                var result = await icmClient.AddOrUpdateIncident2Async(connectorId, icmIncident, RoutingOptions.None);
                                ProcessResult(result, icmIncident);
                            }
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
            catch (Exception ex)
            {
                _logger.Error(ex, "failed_prom2icm.");
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public async Task GenerateIcMFromGrafanaAsync(GrafanaWebhookMessage webhookMessage)
        {
            if (webhookMessage == null)
            {
                throw new ArgumentNullException(nameof(webhookMessage));
            }

            if (webhookMessage.State?.OrdinalEquals("alerting") != true)
            {
                // TODO: We only trigger alerts for now. We can add auto-mitigation later.
                return;
            }

            using var ops = _logger.StartTimedOperation(nameof(GenerateIcMFromGrafanaAsync));
            try
            {
                var icmClient = await _icmClientProvider.GetICMClientAsync();
                if (icmClient == null)
                {
                    _logger.Warning("Cannot get IcM client. Probably IcM is not enabled.");
                    return;
                }

                var icmClientOptions = _icmClientProvider.GetClientOptions();
                var connectorId = Guid.Parse(icmClientOptions.IcmConnectorId);

                var icmIncident = TransformGrafanaAlertToIcmIncident(webhookMessage);

                var incidentSourceId = icmIncident.Source.IncidentId;

                try
                {
                    var existingIncidents = await icmClient.GetIncidentAlertSourceInfo2Async(connectorId, new List<string> { incidentSourceId });
                    if (existingIncidents?.Any() == true)
                    {
                        _logger.Information(
                            "Skip creating new incident, since there are already incidents with the same incidentSourceId '{incidentSourceId}'. IncidentIdList: {@IncidentIdList}",
                            incidentSourceId,
                            existingIncidents.Select(incident => incident.IncidentId));
                    }
                    else
                    {
                        var result = await icmClient.AddOrUpdateIncident2Async(connectorId, icmIncident, RoutingOptions.None);
                        ProcessResult(result, icmIncident);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to submit incident to IcM");

                    if (IsTransientException(e))
                    {
                        // TODO: implement retry.
                        _logger.Warning("Exception is transient and incident submit call should be retried");
                    }

                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "failed_grafana2icm.");
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

        private bool ShouldProcessPrometheusAlert(Alert promAlert)
        {
            if (promAlert?.Labels?.Alertname?.OrdinalEquals("Watchdog") == true)
            {
                // In prometheus, this is an alert meant to ensure that the entire alerting pipeline is functional.
                // This alert is always firing, therefore it should always be firing in Alertmanager
                // and always fire against a receiver.There are integrations with various notification
                // mechanisms that send a notification when this alert is not firing.For example the
                // "DeadMansSnitch" integration in PagerDuty.
                _logger.Information("Watchdog alert is skipped.");
                return false;
            }

            if (promAlert?.Labels?.Alertname?.OrdinalEquals("KubeSchedulerDown") == true)
            {
                // The Kubernetes scheduler is a control plane process which assigns Pods to Nodes.
                // AKS is not running those contraol plane component in customer node pool.
                // https://kubernetes.io/docs/reference/command-line-tools-reference/kube-scheduler/
                _logger.Information("KubeSchedulerDown alert is skipped. kube-scheduler is managed by AKS instead of us users.");
                return false;
            }

            if (promAlert?.Labels?.Alertname?.OrdinalEquals("KubeControllerManagerDown") == true)
            {
                // The Kubernetes controller manager is a daemon that embeds the core control loops shipped with Kubernetes.
                // In applications of robotics and automation, a control loop is a non-terminating loop that regulates the state of the system.
                // AKS is not running those contraol plane component in customer node pool.
                // https://kubernetes.io/docs/reference/command-line-tools-reference/kube-controller-manager/
                _logger.Information("KubeControllerManagerDown alert is skipped. kube-controller-manager is managed by AKS instead of us users.");
                return false;
            }

            if (promAlert?.Labels?.Alertname?.OrdinalEquals("PrometheusNotConnectedToAlertmanagers") == true)
            {
                _logger.Information("PrometheusNotConnectedToAlertmanagers alert is skipped. This could be another prometheus server is not connected. However, since we received this alert here, this mean that it is already working.");
                return false;
            }

            if (promAlert?.Labels?.Alertname?.OrdinalEquals("TargetDown") == true
                && promAlert?.Labels?.Job?.OrdinalEquals("app-pod") == true)
            {
                // For our own metrics collection, we will poll the endpoint no matter if it is enabled.
                return false;
            }

            if (promAlert?.Labels?.Alertname?.OrdinalEquals("KubeAggregatedAPIErrors") == true)
            {
                _logger.Information("KubeAggregatedAPIErrors alert is skipped. This is mostly transient and do not have easy intervention steps.");
                return false;
            }

            if (promAlert?.Labels?.Alertname?.OrdinalEquals("KubeAPIErrorBudgetBurn") == true)
            {
                _logger.Information("KubeAPIErrorBudgetBurn alert is skipped. This is mostly transient and do not have easy intervention steps.");
                return false;
            }

            if (promAlert?.Labels?.Alertname?.OrdinalEquals("KubeAggregatedAPIDown") == true && promAlert?.Annotations?.description?.OrdinalContains("metrics.k8s.io") == true)
            {
                _logger.Information("KubeAggregatedAPIDown alert is skipped. The metrics API is not very critical for us.");
                return false;
            }

            if (promAlert?.Labels?.Alertname?.OrdinalEquals("AlertmanagerFailedToSendAlerts") == true || promAlert?.Labels?.Alertname?.OrdinalEquals("AlertmanagerClusterFailedToSendAlerts") == true)
            {
                _logger.Information($"{promAlert?.Labels?.Alertname} alert is skipped. Probably other alertManager channels are impacted. However. this does matter too much for us science this IcM connector already received the alert.");
                return false;
            }

            if (promAlert?.Labels?.Alertname?.OrdinalEquals("KubeProxyDown") == true)
            {
                _logger.Information("KubeProxyDown alert is skipped. KubeProxy does not have runtime impact on application.");
                return false;
            }

            return true;
        }

        private async Task<AlertSourceIncident> TransformPrometheusAlertToIcmIncidentAsync(PrometheusWebhookMessage webhookMessage, Alert promAlert)
        {
            // Get information about the running Azure compute from the instance metadata service.
            var instanceMeta = await InstanceMetaHelper.GetMetaInfoAsync();
            var incident = PrometheusIncidentMessageGenerator.GenerateIncidentFromPrometheusAlert(webhookMessage, promAlert, instanceMeta, _icmClientProvider.GetClientOptions(), _logger);
            return incident;
        }

        private AlertSourceIncident TransformGrafanaAlertToIcmIncident(GrafanaWebhookMessage webhookMessage)
        {
            // Get information about the running Azure compute from the instance metadata service.
            var incident = GrafanaIncidentMessageGenerator.GenerateIncidentFromGrafanaAlert(webhookMessage, _icmClientProvider.GetClientOptions(), _logger);
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
