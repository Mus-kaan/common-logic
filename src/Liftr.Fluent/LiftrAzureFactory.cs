//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
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
        private readonly LiftrAzureOptions _options;
        private readonly string _tenantId;
        private readonly string _spnObjectId;
        private readonly string _subscriptionId;

        public LiftrAzureFactory(
            ILogger logger,
            string tenantId,
            string spnObjectId,
            string subscriptionId,
            TokenCredential tokenCredential,
            Func<AzureCredentials> credentialsProvider,
            LiftrAzureOptions options = null)
        {
            if (string.IsNullOrEmpty(spnObjectId))
            {
                throw new ArgumentNullException(nameof(spnObjectId));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            _tenantId = tenantId;
            _spnObjectId = spnObjectId;
            _subscriptionId = subscriptionId;
            _logger = logger;
            TokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
            _credentialsProvider = credentialsProvider ?? throw new ArgumentNullException(nameof(credentialsProvider));
            _options = options ?? new LiftrAzureOptions();
        }

        public TokenCredential TokenCredential { get; }

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

            var client = new LiftrAzure(_tenantId, _spnObjectId, TokenCredential, _credentialsProvider.Invoke(), azure, authenticated, _options, _logger);

            return client;
        }
    }
}
