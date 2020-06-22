//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Microsoft.Liftr.Marketplace.Billing.Models
{
    /// <summary>
    /// Marketplace billing API Forbidden response
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis#responses
    /// </summary>
    public class MeteredBillingForbiddenResponse : MeteredBillingRequestResponse
    {
        /// <summary>
        /// Message for forbidden error
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
