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
        private readonly AzureCredentials _credentials;
        private readonly string _subscriptionId;

        public LiftrAzureFactory(AzureCredentials credentials, string subscriptionId, ILogger logger)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            _credentials = credentials;
            _subscriptionId = subscriptionId;
            _logger = logger;
        }

        public ILiftrAzure GenerateLiftrAzure(HttpLoggingDelegatingHandler.Level logLevel = HttpLoggingDelegatingHandler.Level.Basic)
        {
            var authenticated = Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(logLevel)
                    .Authenticate(_credentials);

            var azure = authenticated.WithSubscription(_subscriptionId);

            var client = new LiftrAzure(_credentials, azure, authenticated, _logger);

            return client;
        }
    }
}
