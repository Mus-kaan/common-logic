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
        public TestResourceGroupScope(ILiftrAzure client, string resourceGroupName)
        {
            Client = client;
            ResourceGroupName = resourceGroupName;
        }

        public TestResourceGroupScope(string baseName, ITestOutputHelper output)
            : this(new LiftrAzureFactory(TestCredentials.GetCredentials(), TestCredentials.SubscriptionId, TestLogger.GetLogger(output)).GenerateLiftrAzure(), SdkContext.RandomResourceName(baseName, 25))
        {
        }

        public TestResourceGroupScope(string baseName, NamingContext context, ITestOutputHelper output)
            : this(new LiftrAzureFactory(TestCredentials.GetCredentials(), TestCredentials.SubscriptionId, TestLogger.GetLogger(output)).GenerateLiftrAzure(), context.ResourceGroupName(baseName))
        {
            TestCommon.AddCommonTags(context.Tags);
        }

        public ILiftrAzure Client { get; }

        public NamingContext Naming { get; }

        public string ResourceGroupName { get; }

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
