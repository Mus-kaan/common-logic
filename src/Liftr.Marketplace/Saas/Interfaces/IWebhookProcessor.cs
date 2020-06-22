//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Saas.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Saas.Interfaces
{
    /// <summary>
    /// Webhook flow for Marketplace
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/create-new-saas-offer#technical-configuration
    /// </summary>
    public interface IWebhookProcessor
    {
        /// <summary>
        /// On receiving an update it gets the operation details using operation id
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#implementing-a-webhook-on-the-saas-service
        /// After performing the update it patches the operation to Marketplace to signal success or failure
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#provisioning-for-update-when-its-initiated-from-the-marketplace
        /// </summary>
        /// <param name="payload">Webhook notification payload from the marketplace</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ProcessWebhookNotificationAsync(WebhookPayload payload, CancellationToken cancellationToken = default);
    }
}