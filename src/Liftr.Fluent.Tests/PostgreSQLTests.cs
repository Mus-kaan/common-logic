//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.PostgreSQL.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.Management.PostgreSQL;
using MongoDB.Bson;
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
        public async Task VerifyPosegreSQLServerCreationAsync()
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

        [CheckInValidation(skipLinux: true)]
        public async Task VerifyPostgreSQLFlexibleServerCreationAsync()
        {
            using var scope = new TestResourceGroupScope("ut-pgsql-", _output);
            try
            {
                var azure = scope.Client;
                await azure.RegisterPostgreSQLRPAsync();

                var rg = await azure.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var name = SdkContext.RandomResourceName("tt-pgsql-flexible-", 15);

                var pswd = Guid.NewGuid().ToString();
                var createParameters = new Microsoft.Azure.Management.PostgreSQL.FlexibleServers.Models.Server()
                {
                    AdministratorLogin = "testUser",
                    AdministratorLoginPassword = pswd,
                    HaEnabled = Azure.Management.PostgreSQL.FlexibleServers.Models.HAEnabledEnum.Enabled,
                    Location = TestCommon.Location.Name,
                    StorageProfile = new Azure.Management.PostgreSQL.FlexibleServers.Models.StorageProfile()
                    {
                        BackupRetentionDays = 7,
                        StorageMB = 32768,
                    },
                    Version = "11",
                    Sku = new Azure.Management.PostgreSQL.FlexibleServers.Models.Sku()
                    {
                        Name = "Standard_E2s_v3",
                        Tier = "MemoryOptimized",
                    },
                };

                var server = await azure.CreatePostgreSQLFlexibleServerAsync(rg.Name, name, createParameters);

                var ip = "131.107.159.44";
                await azure.PostgreSQLFlexibleServerAddIPAsync(rg.Name, name, ip);
                await azure.PostgreSQLFlexibleServerAddIPAsync(rg.Name, name, ip);
                await azure.PostgreSQLFlexibleServerRemoveIPAsync(rg.Name, name, ip);
                await azure.PostgreSQLFlexibleServerRemoveIPAsync(rg.Name, name, ip);

                var listResult = await azure.ListPostgreSQLFlexibleServersAsync(rg.Name);
                Assert.Single(listResult);

                var getResult = await azure.GetPostgreSQLFlexibleServerAsync(rg.Name, name);
                Assert.Equal(name, getResult.Name);
            }
            catch (Exception ex)
            {
                scope.Logger.Error(ex, "PostgreSQL flexible server test failed");
                scope.TimedOperation.FailOperation(ex.Message);
                throw;
            }
        }

        // [CheckInValidation(skipLinux: true)]
        [Fact(Skip = "Only local debug for now")]
        public async Task VerifyDatabaseCreationAsync()
        {
            var objectId = ObjectId.GenerateNewId().ToString();
            var dbName = "db_grafana_" + objectId;
            var dbUser = "user_grafana_" + objectId;
            var userPassword = Guid.NewGuid().ToString();
            using var scope = new TestResourceGroupScope("ut-pgsql-", _output);
            try
            {
                var azure = scope.Client;
                await azure.RegisterPostgreSQLRPAsync();

                var sqlOptions = new PostgreSQLOptions()
                {
                    Server = "wuwengpostgreserver1.postgres.database.azure.com",
                    ServerResourceName = "wuwengpostgreserver1",
                    Password = TestPostgreCredentials.AdminPassword,
                };

                var serverClient = new PostgreSQLServerManagement(sqlOptions, scope.Logger);

                await serverClient.CreateUserInNotExistAsync(dbUser, userPassword);
                await serverClient.CreateUserInNotExistAsync(dbUser, userPassword);

                await serverClient.CreateDatabaseIfNotExistAsync(dbName);
                await serverClient.CreateDatabaseIfNotExistAsync(dbName);

                await serverClient.GrantDatabaseAccessAsync(dbName, dbUser);
                await serverClient.GrantDatabaseAccessAsync(dbName, dbUser);

                await serverClient.DropDatabaseAsync(dbName);
                await serverClient.DropDatabaseAsync(dbName);

                await serverClient.DropUserAsync(dbUser);
                await serverClient.DropUserAsync(dbUser);
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
