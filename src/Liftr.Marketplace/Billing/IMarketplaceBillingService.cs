//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Billing.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Billing
{
    /// <summary>
    /// Service contract for Marketplace Metered billing
    /// </summary>
    public interface IMarketplaceBillingService
    {
        /// <summary>
        /// service to Send Usage event to Marketplace for metered billing
        /// </summary>
        /// <param name="marketplaceUsageEventRequest">Request payload for usage event</param>
        /// <param name="requestMetadata">Http request metadata</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Usage event response</returns>
        Task<MeteredBillingRequestResponse> SendUsageEventAsync(UsageEventRequest marketplaceUsageEventRequest, BillingRequestMetadata requestMetadata = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Service to Send Batch usage event to Marketplace for metered billing
        /// </summary>
        /// <param name="marketplaceBatchUsageEventRequest">Request payload for batch usage event</param>
        /// <param name="requestMetadata">Http request metadata</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Batch usage event response</returns>
        Task<MeteredBillingRequestResponse> SendBatchUsageEventAsync(BatchUsageEventRequest marketplaceBatchUsageEventRequest, BillingRequestMetadata requestMetadata = null, CancellationToken cancellationToken = default);
    }
}
