//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using Xunit;

namespace Microsoft.Liftr.Fluent.Contracts.Tests
{
    public class NamingContextTests
    {
        [Fact]
        public void NamingConvertions()
        {
            var context = new NamingContext("TestPartnerCompanyInNYC", "pnyc", EnvironmentType.DogFood, Region.USCentral);
            {
                var name = context.ResourceGroupName("rpdata");
                Assert.Equal("pnyc-df-rpdata-cus-rg", name);
            }

            {
                var name = context.KeyVaultName("kvmoniker");
                Assert.Equal("pnyc-df-kvmoniker-cus-kv", name);
            }

            {
                var name = context.WebAppName("gatewayapp");
                Assert.Equal("pnyc-df-gatewayapp-cus", name);
            }

            {
                var name = context.CosmosDBName("fakedb");
                Assert.Equal("pnyc-df-fakedb-cus-db", name);
            }

            {
                var tags = context.Tags.ToJson();
                Assert.Equal("{\"PartnerName\":\"TestPartnerCompanyInNYC\",\"Environment\":\"DogFood\",\"InfraVersion\":\"v1\",\"RegionTag\":\"centralus\"}", tags);
            }
        }

        [Theory]
        [InlineData("baseName", "role", "baseName-role-vmabc123", "abc123")]
        [InlineData("baseName", "role", "baseName-role-vmdef456", "def456")]
        public void IdentifierFromVMName_ReturnsCorrectIdentifier(string baseName, string role, string vmName, string expectedValue)
        {
            var returnedValue = NamingContext.IdentifierFromVMName(baseName, role, vmName);

            Assert.Equal(returnedValue, expectedValue);
        }
    }
}
