//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.PostgreSQL;
using Microsoft.Azure.Management.PostgreSQL.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Management.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
                using var client = new PostgreSQLManagementClient(azure.AzureCredentials);
                client.SubscriptionId = azure.DefaultSubscriptionId;

                var createParameters = new ServerForCreate(
                    properties: new ServerPropertiesForDefaultCreate(
                        administratorLogin: "testUser",
                        administratorLoginPassword: "testPassword1!"),
                    location: TestCommon.Location.Name,
                    sku: new Microsoft.Azure.Management.PostgreSQL.Models.Sku(name: "B_Gen5_1"));

                var server = await client.Servers.CreateAsync(rg.Name, name, createParameters);
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
