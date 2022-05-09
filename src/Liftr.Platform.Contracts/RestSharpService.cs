//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Platform.Contracts.Interfaces;
using Newtonsoft.Json;
using RestSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Platform.Contracts
{
    public class RestSharpService<T, TResult> : IRestSharpService<T, TResult>
    {
        private readonly ILogger _logger;
        private readonly string _logTag;

        public RestSharpService(ILogger logger, string logTag)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logTag = logTag ?? throw new ArgumentNullException(nameof(logTag));
        }

        public async Task<TResult> CreateAsync(T entity, IDictionary<string, string> headers, Uri endpoint, IDictionary<string, string> parameters = null)
        {
            var httpMethod = Method.POST;
            var restRequest = new RestRequest(httpMethod);
            restRequest.AddJsonBody(entity.ToJsonString());

            return await ExecuteAsync(restRequest, headers, parameters, endpoint, httpMethod);
        }

        public async Task<TResult> GetAsync(IDictionary<string, string> headers, Uri endpoint, IDictionary<string, string> parameters = null)
        {
            var httpMethod = Method.GET;
            var restRequest = new RestRequest(httpMethod);

            return await ExecuteAsync(restRequest, headers, parameters, endpoint, httpMethod);
        }

        public async Task<TResult> DeleteAsync(IDictionary<string, string> headers, Uri endpoint, IDictionary<string, string> parameters = null)
        {
            var httpMethod = Method.DELETE;
            var restRequest = new RestRequest(httpMethod);

            return await ExecuteAsync(restRequest, headers, parameters, endpoint, httpMethod);
        }

        public async Task<TResult> UpdateAsync(T entity, IDictionary<string, string> headers, Uri endpoint, IDictionary<string, string> parameters = null)
        {
            var httpMethod = Method.PATCH;
            var restRequest = new RestRequest(httpMethod);
            restRequest.AddJsonBody(entity.ToJsonString());

            return await ExecuteAsync(restRequest, headers, parameters, endpoint, httpMethod);
        }

        private async Task<TResult> ExecuteAsync(
            RestRequest restRequest,
            IDictionary<string, string> headers,
            IDictionary<string, string> parameters,
            Uri endpoint,
            Method httpMethod)
        {
            ValidateAndAddHeaders(headers, restRequest);
            AddParameters(parameters, restRequest);
            var restClient = new RestClient(endpoint);
            var restResponse = await restClient.ExecuteAsync(restRequest);
            if (restResponse.IsSuccessful)
            {
                _logger.Information($"[{_logTag}] {httpMethod} Execution of entity {nameof(T)}");
                if (!string.IsNullOrWhiteSpace(restResponse.Content))
                {
                    return JsonConvert.DeserializeObject<TResult>(restResponse.Content);
                }
            }

            _logger.Error($"[{_logTag}] {httpMethod} Execution failed for entity {nameof(T)}," +
                $" ResponseCode :{restResponse.StatusCode}, Message: {restResponse.ErrorMessage}");
            return default(TResult);
        }

        private static void ValidateAndAddHeaders(IDictionary<string, string> headers, RestRequest restRequest)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            foreach (var key in headers.Keys)
            {
                restRequest.AddHeader(key, headers[key]);
            }
        }

        private static void AddParameters(IDictionary<string, string> parameters, RestRequest restRequest)
        {
            if (parameters != null)
            {
                foreach (var key in parameters.Keys)
                {
                    restRequest.AddParameter(key, parameters[key]);
                }
            }
        }
    }
}
