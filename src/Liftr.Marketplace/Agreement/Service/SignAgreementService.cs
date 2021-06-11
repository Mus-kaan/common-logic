//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Marketplace.Agreement.Interfaces;
using Microsoft.Liftr.Marketplace.Agreement.Models;
using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Models;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Agreement.Service
{
    public class SignAgreementService : ISignAgreementService
    {
        private readonly ILogger _logger;
        private readonly ISignAgreementRestClient _signAgreementRestClient;

        public SignAgreementService(ISignAgreementRestClient signAgreementRestClient, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _signAgreementRestClient = signAgreementRestClient ?? throw new ArgumentNullException(nameof(signAgreementRestClient));
        }

        public SignAgreementService(ISignAgreementRestClient signAgreementRestClient)
           : this(signAgreementRestClient, LoggerFactory.ConsoleLogger)
        {
        }

        public async Task<AgreementResponse> GetAgreementAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata)
        {
            if (saasResourceProperties is null || !saasResourceProperties.IsValid())
            {
                throw new ArgumentNullException(nameof(saasResourceProperties), $"Please provide valid {nameof(MarketplaceSaasResourceProperties)}");
            }

            if (requestMetadata is null || !requestMetadata.IsValid())
            {
                throw new ArgumentNullException(nameof(requestMetadata), $"Please provide valid {nameof(MarketplaceRequestMetadata)}");
            }

            try
            {
                var resourceTypePath = HttpRequestHelper.GetCompleteRequestPathForAgreement(saasResourceProperties);
                _logger.Information($"Request Path for {nameof(GetAgreementAsync)}: {resourceTypePath}");
                var additionalHeaders = HttpRequestHelper.GetAdditionalMarketplaceHeaders(requestMetadata);
                var agreement = await _signAgreementRestClient.SendRequestAsync<AgreementResponse>(HttpMethod.Get, resourceTypePath, additionalHeaders);
                return agreement;
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"[{nameof(GetAgreementAsync)} Failed to get Agreement response. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                throw;
            }
        }

        public async Task<AgreementResponse> GetandSignAgreementAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata)
        {
            if (saasResourceProperties is null || !saasResourceProperties.IsValid())
            {
                throw new ArgumentNullException(nameof(saasResourceProperties), $"Please provide valid {nameof(MarketplaceSaasResourceProperties)}");
            }

            if (requestMetadata is null || !requestMetadata.IsValid())
            {
                throw new ArgumentNullException(nameof(requestMetadata), $"Please provide valid {nameof(MarketplaceRequestMetadata)}");
            }

            try
            {
                var agreement = await GetAgreementAsync(saasResourceProperties, requestMetadata);
                var signedAgreement = await SignAgreementAsync(saasResourceProperties, requestMetadata, agreement);
                return signedAgreement;
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"[{nameof(GetandSignAgreementAsync)}] Failed to get Agreement response. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                throw;
            }
        }

        public async Task<AgreementResponse> SignAgreementAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata, AgreementResponse request)
        {
            if (saasResourceProperties is null || !saasResourceProperties.IsValid())
            {
                throw new ArgumentNullException(nameof(saasResourceProperties), $"Please provide valid {nameof(MarketplaceSaasResourceProperties)}");
            }

            if (requestMetadata is null || !requestMetadata.IsValid())
            {
                throw new ArgumentNullException(nameof(requestMetadata), $"Please provide valid {nameof(MarketplaceRequestMetadata)}");
            }

            if (request is null || !request.IsValid())
            {
                throw new ArgumentNullException(nameof(request), $"Please provide valid {nameof(AgreementResponse)}");
            }

            if (request.Properties.Accepted)
            {
                return request;
            }

            try
            {
                request.Properties.Accepted = true;
                var resourceTypePath = HttpRequestHelper.GetCompleteRequestPathForAgreement(saasResourceProperties);
                _logger.Information($"Request Path for {nameof(SignAgreementAsync)}: {resourceTypePath}");
                var additionalHeaders = HttpRequestHelper.GetAdditionalMarketplaceHeaders(requestMetadata);
                var agreement = await _signAgreementRestClient.SendRequestAsync<AgreementResponse>(HttpMethod.Put, resourceTypePath, additionalHeaders, content: request.ToJObject());
                return agreement;
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"[{nameof(SignAgreementAsync)}] Failed to get Agreement response. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                throw;
            }
        }
    }
}
