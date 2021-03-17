﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Exceptions;
using System;
using System.Net.Http;

namespace Microsoft.Liftr.Marketplace.Exceptions
{
#nullable enable
    public class PollingException : MarketplaceException
    {
        public PollingException(string message)
            : base(message)
        {
        }

        public PollingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public PollingException()
        {
        }

        public PollingException(
            string message,
            HttpRequestMessage originalRequest,
            Uri operationUri,
            HttpResponseMessage? response = null)
            : base(message)
        {
            if (originalRequest is null)
            {
                throw new ArgumentNullException(nameof(originalRequest));
            }

            OriginalRequestUri = originalRequest.RequestUri;
            PollingOperationUri = operationUri;
            OriginalRequestMethod = originalRequest.Method;
            Response = response;
        }

        public Uri OriginalRequestUri { get; set; } = null!;

        public HttpMethod OriginalRequestMethod { get; set; } = null!;

        public Uri PollingOperationUri { get; set; } = null!;

        public HttpResponseMessage? Response { get; set; }
    }
}
