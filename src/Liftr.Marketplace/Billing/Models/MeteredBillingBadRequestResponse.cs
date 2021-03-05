//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

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
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Name of the target
        /// </summary>
        [JsonProperty("target")]
        public string Target { get; set; }

        /// <summary>
        /// Detail of error
        /// </summary>
        [JsonProperty("details")]
        public IEnumerable<ErrorDetail> Details { get; set; }
    }

    public class ErrorDetail
    {
        /// <summary>
        /// Message for error
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Name of the target
        /// </summary>
        [JsonProperty("target")]
        public string Target { get; set; }

        /// <summary>
        /// code in readable format
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }
    }
}
