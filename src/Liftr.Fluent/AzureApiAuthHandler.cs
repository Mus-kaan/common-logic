//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public class AzureApiAuthHandler : DelegatingHandler
    {
        private readonly AzureCredentials _credentials;
        private readonly SemaphoreSlim _slim = new SemaphoreSlim(1, 1);

        public AzureApiAuthHandler(AzureCredentials credentials)
        {
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await _slim.WaitAsync();
            try
            {
                // AzureCredentials is not thread safe.
                await _credentials.ProcessHttpRequestAsync(request, cancellationToken);
            }
            finally
            {
                _slim.Release();
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
