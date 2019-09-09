//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Liftr.Fluent
{
    public class TracingInterceptor : IServiceClientTracingInterceptor
    {
        private readonly Serilog.ILogger _logger;

        public TracingInterceptor(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public void Information(string message)
        {
            _logger.Verbose(message);
        }

        public void ReceiveResponse(string invocationId, HttpResponseMessage response)
        {
            _logger.Debug($"ReceiveResponse [{invocationId}]" + "Response: {@HttpResponseMessage}. Response Content:{@HttpContent}.", response, response?.Content?.ReadAsStringAsync());
        }

        public void SendRequest(string invocationId, HttpRequestMessage request)
        {
            _logger.Debug($"SendRequest [{invocationId}]" + "Request: {@HttpRequestMessage}", request);
        }

        public void TraceError(string invocationId, Exception exception)
        {
            _logger.Error(invocationId, exception);
        }

        public void Configuration(string source, string name, string value)
        {
        }

        public void EnterMethod(string invocationId, object instance, string method, IDictionary<string, object> parameters)
        {
        }

        public void ExitMethod(string invocationId, object returnValue)
        {
        }
    }
}
