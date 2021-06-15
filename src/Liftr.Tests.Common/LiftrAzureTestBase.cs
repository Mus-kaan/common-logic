//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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

            SubscriptionId = TestCredentails.SubscriptionId;

            ResourceGroupName = $"{TestClassName}-{DateTimeStr}-{s_rand.Next(0, 999)}{TestAzureRegion.ShortName}";

            Location = TestAzureRegion.ToFluentRegion();

            TestResourceGroup = Client.CreateResourceGroup(Location, ResourceGroupName, Tags);
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

        public string SubscriptionId { get; }

        public string ResourceGroupName { get; }

        public IResourceGroup TestResourceGroup { get; }

        public Region Location { get; }

        public Dictionary<string, string> Tags { get; } = new Dictionary<string, string>(TestCommon.Tags);

        public override void Dispose()
        {
            base.Dispose(); // This will find if the test failed.
            if (IsFailure == false)
            {
                // Delete the rg when the test succeeded.
                _ = Client.DeleteResourceGroupAsync(ResourceGroupName);
                Thread.Sleep(3000);
            }

            GC.SuppressFinalize(this);
        }
    }
}
