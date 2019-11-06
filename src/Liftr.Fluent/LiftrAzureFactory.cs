//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Serilog;
using System;

namespace Microsoft.Liftr.Fluent
{
    public class LiftrAzureFactory : ILiftrAzureFactory
    {
        private readonly ILogger _logger;
        private readonly Func<AzureCredentials> _credentialsProvider;
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _subscriptionId;

        public LiftrAzureFactory(ILogger logger, string tenantId, string clientId, string subscriptionId, Func<AzureCredentials> credentialsProvider)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            _tenantId = tenantId;
            _clientId = clientId;
            _subscriptionId = subscriptionId;
            _logger = logger;
            _credentialsProvider = credentialsProvider ?? throw new ArgumentNullException(nameof(credentialsProvider));
        }

        public ILiftrAzure GenerateLiftrAzure(string subscriptionId = null, HttpLoggingDelegatingHandler.Level logLevel = HttpLoggingDelegatingHandler.Level.Basic)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                subscriptionId = _subscriptionId;
            }

            var authenticated = Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(logLevel)
                    .Authenticate(_credentialsProvider.Invoke());

            var azure = authenticated.WithSubscription(subscriptionId);

            var client = new LiftrAzure(_tenantId, _clientId, _credentialsProvider.Invoke(), azure, authenticated, _logger);

            return client;
        }
    }
}
