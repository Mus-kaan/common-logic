//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class CosmosVNetTests : LiftrAzureTestBase
    {
        private const string c_testDBId = "/subscriptions/f885cf14-b751-43c1-9536-dc5b1be02bc0/resourceGroups/cosmosdb-vnet-test-rg-wus2/providers/Microsoft.DocumentDb/databaseAccounts/unittest-vnet-db-210121";

        public CosmosVNetTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [CheckInValidation(skipLinux: true)]
        [PublicEastUS]
        public async Task CanCreateCosmosDBAsync()
        {
            var client = Client;
            var db = await client.GetCosmosDBAsync(c_testDBId);

            Assert.NotNull(db);

            if (!db.VirtualNetoworkFilterEnabled)
            {
                db = await db.Update().WithVirtualNetworkRule("/subscriptions/f885cf14-b751-43c1-9536-dc5b1be02bc0/resourceGroups/cosmosdb-vnet-test-rg-wus2/providers/Microsoft.Network/virtualNetworks/ut-dbvnet-wus2-210121", "default").ApplyAsync();
            }

            var vnetRules = db.VirtualNetworkRules.ToList();

            db = await db.TurnOffVNetAsync(client);
            Assert.False(db.VirtualNetoworkFilterEnabled);

            await Task.Delay(TimeSpan.FromSeconds(5));

            db = await db.TurnOnVNetAsync(vnetRules);
            Assert.True(db.VirtualNetoworkFilterEnabled);
        }
    }
}
}
