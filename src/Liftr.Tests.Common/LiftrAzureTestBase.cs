//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Tests
{
    public class LiftrAzureTestBase : LiftrTestBase
    {
        private static readonly Random s_rand = new Random();

        public LiftrAzureTestBase(ITestOutputHelper output, bool useMethodName = false, [CallerFilePath] string sourceFile = "")
           : base(output, useMethodName, sourceFile)
        {
            if (TestCloudType == null)
            {
                throw new InvalidOperationException("Cannot find cloud type, please make sure the test method is marked by the cloud region test trait. e.g. 'PublicWestUS2'");
            }

            if (TestAzureRegion == null)
            {
                throw new InvalidOperationException("Cannot find azure region, please make sure the test method is marked by the cloud region test trait. e.g. 'PublicWestUS2'");
            }

            TestCredentails = TestCredentailsLoader.LoadTestCredentails(TestCloudType.Value, Logger);

            AzFactory = TestCredentails.AzFactory;

            ResourceGroupName = $"{TestClassName}-{DateTimeStr}-{s_rand.Next(0, 999)}{TestAzureRegion.ShortName}";

            TestResourceGroup = Client.CreateResourceGroupAsync(TestAzureRegion.ToFluentRegion(), ResourceGroupName, TestCommon.Tags).Result;
        }

        public TestCredentails TestCredentails { get; }

        public LiftrAzureFactory AzFactory { get; }

        public ILiftrAzure Client
        {
            get
            {
                return AzFactory.GenerateLiftrAzure();
            }
        }

        public string ResourceGroupName { get; }

        public IResourceGroup TestResourceGroup { get; }

        public override void Dispose()
        {
            base.Dispose(); // This will find if the test failed.
            if (IsFailure == false)
            {
                // Delete the rg when the test succeeded.
                _ = Client.DeleteResourceGroupAsync(ResourceGroupName);
                Task.Delay(3000).Wait();
            }

            GC.SuppressFinalize(this);
        }
    }
}
