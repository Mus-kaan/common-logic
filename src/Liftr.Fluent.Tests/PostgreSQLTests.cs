//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.PostgreSQL.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Management.PostgreSQL;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class PostgreSQLTests
    {
        private readonly ITestOutputHelper _output;

        public PostgreSQLTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task VerifyPosegreSQLCreationAsync()
        {
            using var scope = new TestResourceGroupScope("ut-pgsql-", _output);
            try
            {
                var azure = scope.Client;
                await azure.RegisterPostgreSQLRPAsync();

                var rg = await azure.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var name = SdkContext.RandomResourceName("tt-pgsql-", 15);

                var pswd = Guid.NewGuid().ToString();
                var createParameters = new ServerForCreate(
                    properties: new ServerPropertiesForDefaultCreate(
                        administratorLogin: "testUser",
                        administratorLoginPassword: pswd),
                    location: TestCommon.Location.Name,
                    sku: new Sku(name: "B_Gen5_1"));

                var server = await azure.CreatePostgreSQLServerAsync(rg.Name, name, createParameters);

                var ip = "131.107.159.44";
                await azure.PostgreSQLAddIPAsync(rg.Name, name, ip);
                await azure.PostgreSQLAddIPAsync(rg.Name, name, ip);
                await azure.PostgreSQLRemoveIPAsync(rg.Name, name, ip);
                await azure.PostgreSQLRemoveIPAsync(rg.Name, name, ip);

                var listResult = await azure.ListPostgreSQLServersAsync(rg.Name);
                Assert.Single(listResult);

                var getResult = await azure.GetPostgreSQLServerAsync(rg.Name, name);
                Assert.Equal(name, getResult.Name);
            }
            catch (Exception ex)
            {
                scope.Logger.Error(ex, "PostgreSQL test failed");
                scope.TimedOperation.FailOperation(ex.Message);
                throw;
            }
        }
    }
}
