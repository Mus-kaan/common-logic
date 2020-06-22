//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Liftr.Marketplace.Billing.Models
{
    /// <summary>
    /// Marketplace billing API Batch Success response
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis#responses-1
    /// </summary>
    public class MeteredBillingBatchUsageSuccessResponse : MeteredBillingRequestResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("result")]
        public IEnumerable<BatchAdditionInfoModel> Result { get; set; }
    }

    public class BatchAdditionInfoModel : AcceptedMessage
    {
        [JsonPropertyName("error")]
        public object Error { get; set; } = null;
    }
}
