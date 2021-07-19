//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Contracts;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public class LiftrAzureFactory : ILiftrAzureFactory
    {
        private readonly ILogger _logger;
        private readonly Func<AzureCredentials> _credentialsProvider;
        private readonly LiftrAzureOptions _options;
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
        : this(logger, tenantId, spnObjectId, subscriptionId, tokenCredential, credentialsProvider, options, checkSpn: true)
        {
        }

        private LiftrAzureFactory(
            ILogger logger,
            string tenantId,
            string spnObjectId,
            string subscriptionId,
            TokenCredential tokenCredential,
            Func<AzureCredentials> credentialsProvider,
            LiftrAzureOptions options = null,
            bool checkSpn = true)
        {
            if (checkSpn && string.IsNullOrEmpty(spnObjectId))
            {
                throw new ArgumentNullException(nameof(spnObjectId));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            TenantId = tenantId;
            _spnObjectId = spnObjectId;
            _subscriptionId = subscriptionId;
            _logger = logger;
            TokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
            _credentialsProvider = credentialsProvider ?? throw new ArgumentNullException(nameof(credentialsProvider));
            _options = options ?? new LiftrAzureOptions();
        }

        public TokenCredential TokenCredential { get; }

        public string TenantId { get; }

        public ILiftrAzure GenerateLiftrAzure(string subscriptionId = null, HttpLoggingDelegatingHandler.Level logLevel = HttpLoggingDelegatingHandler.Level.Basic)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                subscriptionId = _subscriptionId;
            }

            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            var authenticated = Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(logLevel)
                    .Authenticate(_credentialsProvider.Invoke());

            var azure = authenticated.WithSubscription(subscriptionId);

            var client = new LiftrAzure(TenantId, subscriptionId, _spnObjectId, TokenCredential, _credentialsProvider.Invoke(), azure, authenticated, _options, _logger);

            return client;
        }

        public async Task<string> GetStorageConnectionStringAsync(Liftr.Contracts.ResourceId resourceId)
        {
            if (resourceId == null)
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            var az = GenerateLiftrAzure(resourceId.SubscriptionId);
            var stor = await az.GetStorageAccountAsync(resourceId.ResourceGroup, resourceId.ResourceName);
            if (stor == null)
            {
                throw new InvalidOperationException($"Cannot find the storage account with Id '{resourceId}'");
            }

            var storageCredentailManager = new StorageAccountCredentialLifeCycleManager(stor, new SystemTimeSource(), _logger);
            return await storageCredentailManager.GetActiveConnectionStringAsync();
        }
    }
}
