//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class CosmosVNetTests
    {
        private const string c_testDBId = "/subscriptions/f885cf14-b751-43c1-9536-dc5b1be02bc0/resourceGroups/cosmosdb-vnet-test-rg-wus2/providers/Microsoft.DocumentDb/databaseAccounts/unittest-vnet-db-210121";
        private readonly ITestOutputHelper _output;

        public CosmosVNetTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task CanCreateCosmosDBAsync()
        {
            // This test will normally take about 12 minutes.
            using (var scope = new TestResourceGroupScope("unittest-db-", _output))
            {
                try
                {
                    var client = scope.Client;
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
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "test failed");
                    throw;
                }
            }
        }
    }
}
