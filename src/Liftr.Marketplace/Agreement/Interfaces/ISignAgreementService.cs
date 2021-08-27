//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.Marketplace.Agreement.Models;
using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Models;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Agreement.Interfaces
{
    public interface ISignAgreementService
    {
        /// <summary>
        /// Gets Agreement Response
        /// </summary>
        /// <returns>Agreement Response</returns>
        /// <remarks>
        /// </remarks>
        Task<AgreementResponse> GetAgreementAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata);

        /// <summary>
        /// Gets Agreement Response and then Signs the Agreement if not signed
        /// </summary>
        /// <returns>Agreement Response</returns>
        /// <remarks>
        /// </remarks>
        Task<AgreementResponse> GetandSignAgreementAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata);

        /// <summary>
        /// Signs the Agreement if not signed
        /// </summary>
        /// <returns>Agreement Response</returns>
        /// <remarks>
        /// </remarks>
        Task<AgreementResponse> SignAgreementAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata, AgreementResponse request);

        /// <summary>
        /// Gets Agreement Response. Use this function only if you have added Token service auth configurations.
        /// </summary>
        /// <returns>Agreement Response</returns>
        /// <remarks>
        /// </remarks>
        Task<AgreementResponse> GetAgreementUsingTokenServiceAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata);

        /// <summary>
        /// Gets Agreement Response and then Signs the Agreement if not signed. Use this function only if you have added Token service auth configurations.
        /// </summary>
        /// <returns>Agreement Response</returns>
        /// <remarks>
        /// </remarks>
        Task<AgreementResponse> GetandSignAgreementUsingTokenServiceAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata);

        /// <summary>
        /// Signs the Agreement if not signed. Use this function only if you have added Token service auth configurations.
        /// </summary>
        /// <returns>Agreement Response</returns>
        /// <remarks>
        /// </remarks>
        Task<AgreementResponse> SignAgreementUsingTokenServiceAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata, AgreementResponse request);
    }
}
