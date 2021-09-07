//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model;
using System.Collections.Generic;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces
{
    public interface IDiagnosticSettingsManagerResult
    {
        bool SuccessfulOperation { get;  }

        /// <summary>
        /// The reason for the operation status, in case of adding a diagnostic setting.
        /// </summary>
        MonitoringStatusReason Reason { get; }

        /// <summary>
        /// The name of the diagnostic setting associated to the operation.
        /// </summary>
        string DiagnosticSettingsName { get; }

        /// <summary>
        /// The result of of a Single Diagnostic Settings Get
        /// </summary>
        DiagnosticSettingsModel DiagnosticSettingV2Model { get; set; }

        /// <summary>
        /// The result of of a List Diagnostic Settings Get
        /// </summary>
        List<DiagnosticSettingsModel> DiagnosticSettingV2ModelList { get; set; }
    }
}