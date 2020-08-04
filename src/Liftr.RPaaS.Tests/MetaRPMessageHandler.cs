//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.ARM;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.RPaaS.Tests
{
    public class MetaRPMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses =
        new Queue<HttpResponseMessage>();

        public MetaRPMessageHandler()
        {
            SendCalled = 0;
        }

        public int SendCalled { get; set; }

        public void QueueResponse(HttpResponseMessage response) => _responses.Enqueue(response);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await Task.Yield();

            SendCalled++;
            return _responses.Dequeue();
        }
    }
}
