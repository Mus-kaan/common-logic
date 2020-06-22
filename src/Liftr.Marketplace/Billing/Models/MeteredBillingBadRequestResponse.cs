//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Liftr.Marketplace.Billing.Models
{
    /// <summary>
    /// Marketplace billing API Badrequest response
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis#responses
    /// </summary>
    public class MeteredBillingBadRequestResponse : MeteredBillingRequestResponse
    {
        /// <summary>
        /// Message for error
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Name of the target
        /// </summary>
        [JsonPropertyName("target")]
        public string Target { get; set; }

        /// <summary>
        /// Detail of error
        /// </summary>
        [JsonPropertyName("details")]
        public IEnumerable<ErrorDetail> Details { get; set; }
    }

    public class ErrorDetail
    {
        /// <summary>
        /// Message for error
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Name of the target
        /// </summary>
        [JsonPropertyName("target")]
        public string Target { get; set; }

        /// <summary>
        /// code in readable format
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; }
    }
}
