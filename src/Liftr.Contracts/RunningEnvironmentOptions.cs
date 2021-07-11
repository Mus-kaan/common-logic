//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts
{
    public class RunningEnvironmentOptions
    {
        /// <summary>
        /// Application Managed Identity tenant Id
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Application Managed Identity object Id
        /// </summary>
        public string SPNObjectId { get; set; }
    }
}
