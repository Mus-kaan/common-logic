//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Xunit;

namespace Microsoft.Liftr.Fluent.Contracts.Tests
{
    public class NamingContextTests
    {
        [Fact]
        public void DogfoodNamingConvertions()
        {
            var context = new NamingContext("TestPartnerCompanyInNYC", "pnyc", EnvironmentType.DogFood, Region.USCentral);
            {
                var name = context.ResourceGroupName("rpdata");
                Assert.Equal("pnyc-df-rpdata-cus-rg", name);
            }

            {
                var name = context.KeyVaultName("kvmoniker");
                Assert.Equal("pnycdfkvmonikercuskv", name);
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
                Assert.True(tags.OrdinalContains("{\"PartnerName\":\"TestPartnerCompanyInNYC\",\"Environment\":\"DogFood\",\"InfraVersion\":\"v2\",\"RegionTag\":\"centralus\",\"FirstCreatedAt\":"));
            }
        }

        [Fact]
        public void ProdNamingConvertions()
        {
            var context = new NamingContext("Nginx", "ngx", EnvironmentType.Production, Region.USEast);
            {
                var name = context.StorageAccountName("sbi20191001");
                Assert.Equal("stngxprodsbi20191001eus", name);
            }

            {
                var name = context.SharedImageGalleryName("sbi20191001");
                Assert.Equal("ngx_prod_sbi20191001_eus_sig", name);
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
