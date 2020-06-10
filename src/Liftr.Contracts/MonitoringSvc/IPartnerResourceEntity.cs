//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts.MonitoringSvc
{
    /// <summary>
    /// This entity represent partner service resource properties
    /// </summary>
    public interface IPartnerResourceEntity : IResourceEntity, IEncryptionMetaData
    {
        /// <summary>
        /// MonitoredResource's resource type
        /// </summary>
        string ResourceType { get; set; }

        /// <summary>
        /// Partner credential to forward logs to partner service
        /// i.e. in case of datadog, it is api key
        /// </summary>
        string EncryptedContent { get; set; }
    }
}
