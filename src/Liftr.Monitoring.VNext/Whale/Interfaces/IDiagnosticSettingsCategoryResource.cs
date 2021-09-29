//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.VNext.Whale.Models;

namespace Microsoft.Liftr.Monitoring.VNext.Whale.Interfaces
{
    /// <summary>
    /// Diagnostic setting category resource details interface
    /// </summary>
    public interface IDiagnosticSettingsCategoryResource
    {
        /// <summary>
        /// Object id of the azure resource
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Azure resource type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Azure resource name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Property containing the type of the diagnostic settings category
        /// </summary>
        public DiagnosticSettingsCategoryResourceProperties Properties { get; set; }
    }
}
