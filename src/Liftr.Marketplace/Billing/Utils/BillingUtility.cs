//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Billing.Models;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Billing.Utils
{
    public static class BillingUtility
    {
        public static async Task<MeteredBillingRequestResponse> GetMeteredBillingNonSuccessResponseAsync(HttpResponseMessage responseMessage)
        {
            if (responseMessage is null)
            {
                throw new System.ArgumentNullException(nameof(responseMessage));
            }

            switch (responseMessage.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    return await AzureMarketplaceRequestResult.ParseAsync<MeteredBillingBadRequestResponse>(responseMessage);
                case HttpStatusCode.Conflict:
                    return await AzureMarketplaceRequestResult.ParseAsync<MeteredBillingConflictResponse>(responseMessage);
                case HttpStatusCode.Forbidden:
                    return await AzureMarketplaceRequestResult.ParseAsync<MeteredBillingForbiddenResponse>(responseMessage);
                default:
                    return await AzureMarketplaceRequestResult.ParseAsync<MeteredBillingRequestResponse>(responseMessage);
            }
        }
    }
}
