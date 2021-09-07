//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model
{
    public class DiagnosticSettingsManagerResult : IDiagnosticSettingsManagerResult
    {
        public bool SuccessfulOperation { get; set; }

        public MonitoringStatusReason Reason { get; set; }

        public string DiagnosticSettingsName { get; set; }

        public DiagnosticSettingsModel DiagnosticSettingV2Model { get; set; }

        public List<DiagnosticSettingsModel> DiagnosticSettingV2ModelList { get; set; }

        public static DiagnosticSettingsManagerResult SuccessfulResult(MonitoringStatusReason reason = MonitoringStatusReason.CapturedByRules)
        {
            var res = new DiagnosticSettingsManagerResult();
            res.SuccessfulOperation = true;
            res.Reason = reason;
            return res;
        }

        public static DiagnosticSettingsManagerResult FailedResult(MonitoringStatusReason reason = MonitoringStatusReason.Other)
        {
            var res = new DiagnosticSettingsManagerResult();
            res.SuccessfulOperation = false;
            res.Reason = reason;
            return res;
        }
    }
}