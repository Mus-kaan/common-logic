//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class TestResourceGroupScope : IDisposable
    {
        private readonly LiftrAzureFactory _azFactory;
        private readonly ILiftrAzure _client;

        public TestResourceGroupScope(ILiftrAzure client, string resourceGroupName)
        {
            _client = client;
            ResourceGroupName = resourceGroupName;
        }

        public TestResourceGroupScope(string baseName, ITestOutputHelper output)
        {
            _azFactory = new LiftrAzureFactory(TestLogger.GetLogger(output), TestCredentials.SubscriptionId, TestCredentials.GetAzureCredentials);
            ResourceGroupName = SdkContext.RandomResourceName(baseName, 25);
        }

        public TestResourceGroupScope(string baseName, NamingContext namingContext, ITestOutputHelper output)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            _azFactory = new LiftrAzureFactory(TestLogger.GetLogger(output), TestCredentials.SubscriptionId, TestCredentials.GetAzureCredentials);
            ResourceGroupName = namingContext.ResourceGroupName(baseName);
            TestCommon.AddCommonTags(namingContext.Tags);
        }

        public ILiftrAzure Client
        {
            get
            {
                return _client ?? _azFactory.GenerateLiftrAzure();
            }
        }

        public NamingContext Naming { get; }

        public string ResourceGroupName { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public void Dispose()
        {
            try
            {
                var deleteTask = Client.DeleteResourceGroupAsync(ResourceGroupName);
                Task.Yield();
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
                Task.Delay(2000).Wait();
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
            }
            catch
            {
            }
        }
    }
}
