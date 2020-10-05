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
        public AzureMarketplaceRequestResult()
        {
            Success = false;
        }

        [JsonIgnore]
        public string RawResponse { get; internal set; }

        [JsonIgnore]
        public Guid RequestId { get; set; }

        [JsonIgnore]
        public Guid CorrelationId { get; set; }

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

            if (!string.IsNullOrWhiteSpace(jsonString) && response.StatusCode != HttpStatusCode.Forbidden)
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

        public static string GetIdHeaderValue(HttpHeaders headers, string keyName)
        {
            if (headers is null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            return headers.TryGetValues(keyName, out var values) ? values.FirstOrDefault() : Guid.Empty.ToString();
        }

        protected virtual void UpdateFromHeaders(HttpHeaders headers)
        {
            if (headers is null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            RequestId = Guid.Parse(GetIdHeaderValue(headers, MarketplaceConstants.BillingRequestIdHeaderKey));
            CorrelationId = Guid.Parse(GetIdHeaderValue(headers, MarketplaceConstants.BillingCorrelationIdHeaderKey));
        }
    }
}