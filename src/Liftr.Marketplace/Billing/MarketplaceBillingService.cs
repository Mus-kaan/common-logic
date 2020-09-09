//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Billing.Contracts;
using Microsoft.Liftr.Marketplace.Billing.Models;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Billing
{
    public class MarketplaceBillingService : IMarketplaceBillingService
    {
        private readonly IMarketplaceBillingClient _marketplaceBillingClient;
        private readonly ILogger _logger;

        public MarketplaceBillingService(IMarketplaceBillingClient marketplaceBillingClient, ILogger logger)
        {
            _marketplaceBillingClient = marketplaceBillingClient ?? throw new ArgumentNullException(nameof(marketplaceBillingClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Service to Send Usage event to Marketplace for metered billing
        /// </summary>
        /// <param name="marketplaceUsageEventRequest">Request payload for usage event</param>
        /// <param name="requestMetadata">Http request metadata</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Usage event response</returns>
        public async Task<MeteredBillingRequestResponse> SendUsageEventAsync(UsageEventRequest marketplaceUsageEventRequest, BillingRequestMetadata requestMetadata = null, CancellationToken cancellationToken = default)
        {
            if (marketplaceUsageEventRequest is null)
            {
                throw new ArgumentNullException(nameof(marketplaceUsageEventRequest));
            }

            var validationResponse = marketplaceUsageEventRequest.Validate();
            if (!validationResponse.Success)
            {
                return await Task.FromResult(validationResponse);
            }
            else
            {
                return await _marketplaceBillingClient.SendUsageEventAsync(marketplaceUsageEventRequest, requestMetadata, cancellationToken);
            }
        }

        /// <summary>
        /// Service to Send Batch usage event to Marketplace for metered billing
        /// </summary>
        /// <param name="marketplaceBatchUsageEventRequest">Request payload for batch usage event</param>
        /// <param name="requestMetadata">Http request metadata</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Batch usage event response</returns>
        public async Task<MeteredBillingRequestResponse> SendBatchUsageEventAsync(BatchUsageEventRequest marketplaceBatchUsageEventRequest, BillingRequestMetadata requestMetadata = null, CancellationToken cancellationToken = default)
        {
            if (marketplaceBatchUsageEventRequest is null)
            {
                throw new ArgumentNullException(nameof(marketplaceBatchUsageEventRequest));
            }

            var validationResponse = marketplaceBatchUsageEventRequest.Validate();
            if (!validationResponse.Success)
            {
                return await Task.FromResult(validationResponse);
            }
            else
            {
                return await _marketplaceBillingClient.SendBatchUsageEventAsync(marketplaceBatchUsageEventRequest, requestMetadata, cancellationToken);
            }
        }
    }
}
