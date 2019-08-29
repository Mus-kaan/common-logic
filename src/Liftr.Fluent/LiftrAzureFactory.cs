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

        public LiftrAzureFactory(AzureCredentials credentials, ILogger logger)
            : this(credentials, credentials.DefaultSubscriptionId, logger)
        {
        }

        public ILiftrAzure GenerateLiftrAzure(HttpLoggingDelegatingHandler.Level logLevel = HttpLoggingDelegatingHandler.Level.Basic)
        {
            var azure = Azure.Management.Fluent.Azure
                    .Configure()
                    .WithDelegatingHandler(new HttpLoggingDelegatingHandler())
                    .WithLogLevel(logLevel)
                    .Authenticate(_credentials)
                    .WithSubscription(_subscriptionId);

            var client = new LiftrAzure(_credentials, azure, _logger);

            return client;
        }
    }
}
