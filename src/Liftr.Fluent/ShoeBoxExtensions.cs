//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public static class ShoeBoxExtensions
    {
        public const string c_diagSettingsName = "centralized-log-analytics";

        public static Task ExportDiagnosticsToLogAnalyticsAsync(this ILiftrAzure liftrAzure, IVault kv, string logAnalyticsWorkspaceId)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (kv == null)
            {
                throw new ArgumentNullException(nameof(kv));
            }

            return liftrAzure.FluentClient.DiagnosticSettings
                    .Define(c_diagSettingsName)
                    .WithResource(kv.Id)
                    .WithLogAnalytics(logAnalyticsWorkspaceId)
                    .WithLog("AuditEvent", 365)
                    .WithMetric("AllMetrics", TimeSpan.FromHours(1), 365)
                    .CreateAsync();
        }

        public static Task ExportDiagnosticsToLogAnalyticsAsync(this ILiftrAzure liftrAzure, IRegistry acr, string logAnalyticsWorkspaceId)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (acr == null)
            {
                throw new ArgumentNullException(nameof(acr));
            }

            return liftrAzure.FluentClient.DiagnosticSettings
                    .Define(c_diagSettingsName)
                    .WithResource(acr.Id)
                    .WithLogAnalytics(logAnalyticsWorkspaceId)
                    .WithLog("ContainerRegistryRepositoryEvents", 365)
                    .WithLog("ContainerRegistryLoginEvents", 365)
                    .WithMetric("AllMetrics", TimeSpan.FromHours(1), 365)
                    .CreateAsync();
        }

        public static Task ExportDiagnosticsToLogAnalyticsAsync(this ILiftrAzure liftrAzure, ICosmosDBAccount db, string logAnalyticsWorkspaceId)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            return liftrAzure.FluentClient.DiagnosticSettings
                    .Define(c_diagSettingsName)
                    .WithResource(db.Id)
                    .WithLogAnalytics(logAnalyticsWorkspaceId)
                    .WithLog("DataPlaneRequests", 365)
                    .WithLog("MongoRequests", 365)
                    .WithLog("QueryRuntimeStatistics", 365)
                    .WithLog("PartitionKeyStatistics", 365)
                    .WithLog("PartitionKeyRUConsumption", 365)
                    .WithLog("ControlPlaneRequests", 365)
                    .WithMetric("Requests", TimeSpan.FromHours(1), 365)
                    .CreateAsync();
        }

        public static Task ExportDiagnosticsToLogAnalyticsAsync(this ILiftrAzure liftrAzure, ITrafficManagerProfile tm, string logAnalyticsWorkspaceId)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (tm == null)
            {
                throw new ArgumentNullException(nameof(tm));
            }

            return liftrAzure.FluentClient.DiagnosticSettings
                    .Define(c_diagSettingsName)
                    .WithResource(tm.Id)
                    .WithLogAnalytics(logAnalyticsWorkspaceId)
                    .WithLog("ProbeHealthStatusEvents", 365)
                    .WithMetric("AllMetrics", TimeSpan.FromHours(1), 365)
                    .CreateAsync();
        }

        public static Task ExportDiagnosticsToLogAnalyticsAsync(this ILiftrAzure liftrAzure, INetwork vnet, string logAnalyticsWorkspaceId)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (vnet == null)
            {
                throw new ArgumentNullException(nameof(vnet));
            }

            return liftrAzure.FluentClient.DiagnosticSettings
                    .Define(c_diagSettingsName)
                    .WithResource(vnet.Id)
                    .WithLogAnalytics(logAnalyticsWorkspaceId)
                    .WithLog("VMProtectionAlerts", 365)
                    .WithMetric("AllMetrics", TimeSpan.FromHours(1), 365)
                    .CreateAsync();
        }

        public static Task ExportDiagnosticsToLogAnalyticsAsync(this ILiftrAzure liftrAzure, IKubernetesCluster aks, string logAnalyticsWorkspaceId)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (aks == null)
            {
                throw new ArgumentNullException(nameof(aks));
            }

            return liftrAzure.FluentClient.DiagnosticSettings
                    .Define(c_diagSettingsName)
                    .WithResource(aks.Id)
                    .WithLogAnalytics(logAnalyticsWorkspaceId)
                    .WithLog("kube-apiserver", 365)
                    .WithLog("kube-controller-manager", 365)
                    .WithLog("kube-scheduler", 365)
                    .WithLog("cluster-autoscaler", 365)
                    .WithLog("guard", 365)
                    .WithMetric("AllMetrics", TimeSpan.FromHours(1), 365)
                    .CreateAsync();
        }
    }
}
