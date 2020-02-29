//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.RPaaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.RPaaS.Tests
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await Task.Yield();

            var listResponse1 = new ListResponse<TestResource>()
            {
                Value = new List<TestResource>() { Constants.Resource1() },
                NextLink = Constants.NextLink,
            };

            var listResponse2 = new ListResponse<TestResource>()
            {
                Value = new List<TestResource>() { Constants.Resource2() },
                NextLink = null,
            };

            if (ValidateRequest1(request))
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(listResponse1.ToJson()),
                };
            }
            else if (ValidateRequest2(request))
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(listResponse2.ToJson()),
                };
            }
            else
            {
                throw new ArgumentException("Invalid request.");
            }
        }

        private static bool ValidateRequest1(HttpRequestMessage request)
        {
            return request.Method == HttpMethod.Get && request.RequestUri.ToString() == Constants.FullEndpoint;
        }

        private static bool ValidateRequest2(HttpRequestMessage request)
        {
            return request.Method == HttpMethod.Get && request.RequestUri.ToString() == Constants.NextLink;
        }
    }
}
