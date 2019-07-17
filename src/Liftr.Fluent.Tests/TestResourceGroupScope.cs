//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class TestResourceGroupScope : IDisposable
    {
        public TestResourceGroupScope(string baseName, ITestOutputHelper output)
        {
            Client = new AzureClient(TestCredentials.GetAzure(), TestCredentials.ClientId, TestCredentials.ClientSecret, TestLogger.GetLogger(output));
            ResourceGroupName = SdkContext.RandomResourceName(baseName, 25);
        }

        public TestResourceGroupScope(AzureClient client, string resourceGroupName)
        {
            Client = client;
            ResourceGroupName = resourceGroupName;
        }

        public AzureClient Client { get; }

        public string ResourceGroupName { get; }

        public void Dispose()
        {
            try
            {
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
                Client.DeleteResourceGroupAsync(ResourceGroupName).Wait();
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
            }
            catch
            {
            }
        }
    }
}
