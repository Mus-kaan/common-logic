//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Billing.Models
{
    public class AzureMarketplaceRequestResult
    {
        private const string RequestIdKey = "x-ms-requestid";

        public AzureMarketplaceRequestResult()
        {
            Success = false;
        }

        [JsonIgnore]
        public string RawResponse { get; internal set; }

        [JsonIgnore]
        public Guid RequestId { get; set; }

        [JsonIgnore]
        public HttpStatusCode StatusCode { get; set; }

        [JsonIgnore]
        public bool Success { get; set; }

        public static async Task<T> ParseAsync<T>(HttpResponseMessage response) where T : AzureMarketplaceRequestResult, new()
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            var jsonString = await response.Content.ReadAsStringAsync();

            T result;

            if (!string.IsNullOrWhiteSpace(jsonString))
            {
                result = JsonConvert.DeserializeObject<T>(jsonString);
            }
            else
            {
                result = (T)Convert.ChangeType(new T(), typeof(T), CultureInfo.InvariantCulture);
            }

            result.RawResponse = jsonString;

            result.StatusCode = response.StatusCode;

            result.UpdateFromHeaders(response.Headers);

            result.Success = response.IsSuccessStatusCode;

            return result;
        }

        protected virtual void UpdateFromHeaders(HttpHeaders headers)
        {
            if (headers is null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            if (headers.TryGetValues(RequestIdKey, out var values))
            {
                RequestId = Guid.Parse(values.First());
            }
        }
    }
}
