//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces;
using Microsoft.Liftr.Contracts.MonitoringSvc;

namespace Liftr.Monitoring.VNext.DiagnosticSettings
{
    public class DiagnosticSettingsNameProvider : IDiagnosticSettingsNameProvider
    {
        private string DiagnosticNamePrefix;
        public string PrefixedResourceProvider;

        public DiagnosticSettingsNameProvider(MonitoringResourceProvider monitoringResourceProvider)
        {
            DiagnosticNamePrefix = monitoringResourceProvider.ToString().ToUpperInvariant();
            PrefixedResourceProvider = "providers/Microsoft." + monitoringResourceProvider.ToString();
        }

        public string GetDiagnosticSettingNameForResourceV2()
        {
            return $"{DiagnosticNamePrefix}_DS_V2_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        public string GetPrefixedResourceProviderName()
        {
            return PrefixedResourceProvider;
        }
    }
}