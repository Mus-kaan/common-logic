//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.TokenManager;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Whale
{
    /// <summary>
    /// This class is used to override default Fluent authentication. It allows us to override
    /// the headers and set the correct token and the auxiliary token for cross-tenant requests.
    /// </summary>
    public class WhaleDelegatingHandler : DelegatingHandler
    {
        private const string AuxiliaryTokenHeader = "x-ms-authorization-auxiliary";
        private readonly ITokenManager _tokenManager;
        private readonly string _firstPartyClientId;
        private readonly X509Certificate2 _firstPartyCertificate;

        public WhaleDelegatingHandler(
            ITokenManager tokenManager,
            string firstPartyClientId,
            X509Certificate2 firstPartyCertificate)
            : base()
        {
            if (string.IsNullOrWhiteSpace(firstPartyClientId))
            {
                throw new ArgumentNullException(nameof(firstPartyClientId));
            }

            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
            _firstPartyClientId = firstPartyClientId;
            _firstPartyCertificate = firstPartyCertificate ?? throw new ArgumentNullException(nameof(firstPartyCertificate));
        }

        protected async override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // We need to set up the auxiliary token to allow linking a managed event hub to the
            // diagnostic settings. We should do this only if it's a PUT request.
            if (request.Method == HttpMethod.Put)
            {
                // No need to specify the tenant as we want to load the token from the default one
                var auxiliaryToken = await _tokenManager.GetTokenAsync(
                    _firstPartyClientId, _firstPartyCertificate, sendX5c: true);

                request.Headers.Add(AuxiliaryTokenHeader, $"Bearer {auxiliaryToken}");
            }

            // We send the message using the base method.
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
