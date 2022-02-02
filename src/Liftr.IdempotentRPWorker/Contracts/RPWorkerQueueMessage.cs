//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    public class RPWorkerQueueMessage
    {
        /// <summary>
        /// The command associated with the message.
        /// </summary>
        public RPWorkerQueueCommandEnum Command { get; set; }

        /// <summary>
        /// The id of the operation associated to the message.
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Tenant Id of the resource
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Azure SubscriptionId Id used for creating resource
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// API Version
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        /// Accept Language Header Value for Localization
        /// </summary>
        public string LanguageHeaderValue { get; set; } = "en";

        /// <summary>
        /// The definition of the resource associated to the message.
        /// </summary>
        public BaseResource Resource { get; set; }

        /// <summary>
        /// List of required request headers for service to service calls.
        /// </summary>
        public PartnerRequestMetaData RequestMetadata { get; set; }
    }
}
