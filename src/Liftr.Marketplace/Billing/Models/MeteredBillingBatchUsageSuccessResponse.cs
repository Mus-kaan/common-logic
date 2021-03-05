//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.Marketplace.Billing.Models
{
    /// <summary>
    /// Marketplace billing API Batch Success response
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis#responses-1
    /// </summary>
    public class MeteredBillingBatchUsageSuccessResponse : MeteredBillingRequestResponse
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("result")]
        public IEnumerable<BatchAdditionInfoModel> Result { get; set; }
    }

    public class BatchAdditionInfoModel : AcceptedMessage
    {
        [JsonProperty("error")]
        public object Error { get; set; } = null;
    }
}
