//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Microsoft.Liftr.Marketplace.Billing.Models
{
    public class UsageEventRequest
    {
        /// <summary>
        /// Identifier of the resource against which usage is emitted
        /// </summary>
        [JsonProperty("resourceId")]
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Quantity used
        /// </summary>
        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        /// <summary>
        /// Dimension identifier
        /// </summary>
        [JsonProperty("dimension")]
        public string Dimension { get; set; }

        /// <summary>
        /// Time in UTC when the usage event occurred
        /// </summary>
        [JsonProperty("effectiveStartTime")]
        public DateTime EffectiveStartTime { get; set; }

        /// <summary>
        /// Plan associated with the purchased offer
        /// </summary>
        [JsonProperty("planId")]
        public string PlanId { get; set; }

        public MeteredBillingRequestResponse Validate()
        {
            var validationDetails = new List<ErrorDetail>();

            if (ResourceId == Guid.Empty)
            {
                validationDetails.Add(new ErrorDetail
                {
                    Message = $"{nameof(ResourceId)} cannot be null or empty.",
                    Code = "ValidationError",
                    Target = nameof(ResourceId),
                });
            }

            if (Quantity <= 0)
            {
                validationDetails.Add(new ErrorDetail
                {
                    Message = $"{nameof(Quantity)} cannot be 0 or less than 0.",
                    Code = "ValidationError",
                    Target = nameof(Quantity),
                });
            }

            if (string.IsNullOrWhiteSpace(Dimension))
            {
                validationDetails.Add(new ErrorDetail
                {
                    Message = $"{nameof(Dimension)} cannot be null or empty.",
                    Code = "ValidationError",
                    Target = nameof(Dimension),
                });
            }

            if (validationDetails.Any())
            {
                return new MeteredBillingBadRequestResponse
                {
                    Code = "ValidationError",
                    Message = "One or more validation error occured.",
                    StatusCode = HttpStatusCode.BadRequest,
                    Details = validationDetails,
                    RawResponse = JsonConvert.SerializeObject(this),
                    Success = false,
                    Target = MarketplaceConstants.BillingUsageEventPath,
                };
            }
            else
            {
                return new MeteredBillingRequestResponse
                {
                    Success = true,
                };
            }
        }
    }
}
