//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces
{
    public interface IDiagnosticSettingsManager
    {
        Task<IDiagnosticSettingsManagerResult> GetResourceDiagnosticSettingsAsync(string diagnosticSettingsId, string tenantId);

        Task<IDiagnosticSettingsManagerResult> ListResourceDiagnosticSettingsAsync(string monitoredResourceId, string tenantId);

        Task<IDiagnosticSettingsManagerResult> CreateOrUpdateResourceDiagnosticSettingAsync(string monitoredResourceId, string monitorId, string tenantId);

        Task<IDiagnosticSettingsManagerResult> CreateOrUpdateResourceDiagnosticSettingAsync(string monitoredResourceId, string diagnosticSettingsName, string monitorId, string tenantId);

        Task<IDiagnosticSettingsManagerResult> RemoveResourceDiagnosticSettingAsync(string monitoredResourceId, string diagnosticSettingName, string tenantId);

        Task<IDiagnosticSettingsManagerResult> CreateOrUpdateSubscriptionDiagnosticSettingAsync(string monioredSubscriptionId, string monitorId, string tenantId);

        Task<IDiagnosticSettingsManagerResult> CreateOrUpdateSubscriptionDiagnosticSettingAsync(string monioredSubscriptionId, string diagnosticSettingsName, string monitorId, string tenantId);

        Task<IDiagnosticSettingsManagerResult> RemoveSubscriptionDiagnosticSettingAsync(string monioredSubscriptionId, string diagnosticSettingName, string monitorId, string tenantId);
    }
}