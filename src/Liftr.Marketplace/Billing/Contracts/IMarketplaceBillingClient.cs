//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Billing.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Billing.Contracts
{
    /// <summary>
    /// Client used to make the Marketplace Metered billing Requests
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis
    /// </summary>
    public interface IMarketplaceBillingClient
    {
        /// <summary>
        /// Send Usage event to Marketplace for metered billing
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis#usage-event
        /// </summary>
        /// <param name="marketplaceUsageEventRequest">Request payload for usage event</param>
        /// <param name="requestMetadata">Http request metadata</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Usage event response</returns>
        Task<MeteredBillingRequestResponse> SendUsageEventAsync(UsageEventRequest marketplaceUsageEventRequest, BillingRequestMetadata requestMetadata = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send Batch usage event to Marketplace for metered billing
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis#batch-usage-event
        /// </summary>
        /// <param name="marketplaceBatchUsageEventRequest">Request payload for batch usage event</param>
        /// <param name="requestMetadata">Http request metadata</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Batch usage event response</returns>
        Task<MeteredBillingRequestResponse> SendBatchUsageEventAsync(BatchUsageEventRequest marketplaceBatchUsageEventRequest, BillingRequestMetadata requestMetadata = null, CancellationToken cancellationToken = default);
    }
}
