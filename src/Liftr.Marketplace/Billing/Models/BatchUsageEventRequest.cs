//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;

namespace Microsoft.Liftr.Marketplace.Billing.Models
{
    public class BatchUsageEventRequest
    {
        [JsonPropertyName("request")]
        public IEnumerable<UsageEventRequest> Request { get; set; }

        public MeteredBillingRequestResponse Validate()
        {
            var validationDetails = new List<ErrorDetail>();

            if (Request != null || Request.Any())
            {
                int i = 1;
                foreach (var request in Request)
                {
                    var validateResponse = request.Validate();
                    if (!validateResponse.Success)
                    {
                        var badRequest = (MeteredBillingBadRequestResponse)validateResponse;
                        validationDetails.AddRange(badRequest.Details.Select(x => new ErrorDetail
                        {
                            Code = x.Code,
                            Message = $"Req {i}: {x.Message}",
                            Target = x.Target,
                        }));
                    }

                    i++;
                }
            }
            else
            {
                validationDetails.Add(new ErrorDetail
                {
                    Message = "Requests cannot be null or empty.",
                    Code = "ValidationError",
                    Target = nameof(Request),
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
                    Target = Constants.BatchUsageEventPath,
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
