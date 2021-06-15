//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.PostgreSQL.FlexibleServers;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Management.PostgreSQL;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class PostgreSQLFlexibleServerTests : LiftrAzureTestBase
    {
        public PostgreSQLFlexibleServerTests(ITestOutputHelper output)
            : base(output, useMethodName: true)
        {
        }

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS2]
        public async Task VerifyPostgreSQLFlexibleServerCreationAsync()
        {
            try
            {
                var azure = Client;
                var name = SdkContext.RandomResourceName("tt-pgsql-flexible-", 15);

                var adminUser = "testUser";
                var adminPassword = Guid.NewGuid().ToString();
                var createParameters = new Azure.Management.PostgreSQL.FlexibleServers.Models.Server()
                {
                    AdministratorLogin = adminUser,
                    AdministratorLoginPassword = adminPassword,
                    HaEnabled = Azure.Management.PostgreSQL.FlexibleServers.Models.HAEnabledEnum.Enabled,
                    Location = Location.Name,
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

                var server = await azure.CreatePostgreSQLFlexibleServerAsync(ResourceGroupName, name, createParameters);

                using (var client = azure.GetPostgreSQLFlexibleServerClient())
                {
                    // open the db to all for testing.
                    var newRule = new Azure.Management.PostgreSQL.FlexibleServers.Models.FirewallRule("0.0.0.0", "255.255.255.255");
                    await client.FirewallRules.CreateOrUpdateAsync(ResourceGroupName, name, "all", newRule);
                }

                // All the following management calls are called twice.
                // The code cosumer (RP worker) can be ephemeral, it may restart in the middle of doing some work.
                // Calling the method multiple times should just have no effect without exceptions.
                var ip = "131.107.159.44";
                await azure.PostgreSQLFlexibleServerAddIPAsync(ResourceGroupName, name, ip);
                await azure.PostgreSQLFlexibleServerAddIPAsync(ResourceGroupName, name, ip);
                await azure.PostgreSQLFlexibleServerRemoveIPAsync(ResourceGroupName, name, ip);
                await azure.PostgreSQLFlexibleServerRemoveIPAsync(ResourceGroupName, name, ip);

                var listResult = await azure.ListPostgreSQLFlexibleServersAsync(ResourceGroupName);
                Assert.Single(listResult);

                var getResult = await azure.GetPostgreSQLFlexibleServerAsync(ResourceGroupName, name);
                Assert.Equal(name, getResult.Name);

                var objectId = ObjectId.GenerateNewId().ToString();
                var dbName = "db_grafana_" + objectId;
                var dbUser = "user_grafana_" + objectId;
                var userPassword = Guid.NewGuid().ToString();

                var sqlOptions = new PostgreSQLOptions()
                {
                    Server = getResult.FullyQualifiedDomainName,
                    ServerResourceName = getResult.Name,
                    Username = adminUser,
                    Password = adminPassword,
                };

                var options = new PostgreSQLServerManagementOptions()
                {
                    ServerAdminUsername = sqlOptions.Username,
                    ConnectionString = sqlOptions.FlexibleServerConnectionString,
                };

                var serverClient = new PostgreSQLServerManagement(options, Logger);

                try
                {
                    await serverClient.CreateUserIfNotExistAsync(dbUser, userPassword);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "add user failed");
                    throw;
                }

                // All the following management calls are called twice.
                // The code cosumer (RP worker) can be ephemeral, it may restart in the middle of doing some work.
                // Calling the method multiple times should just have no effect without exceptions.
                await serverClient.CreateUserIfNotExistAsync(dbUser, userPassword);

                await serverClient.CreateDatabaseIfNotExistAsync(dbName);
                await serverClient.CreateDatabaseIfNotExistAsync(dbName);

                await serverClient.GrantDatabaseAccessAsync(dbName, dbUser);
                await serverClient.GrantDatabaseAccessAsync(dbName, dbUser);

                await serverClient.DropDatabaseAsync(dbName);
                await serverClient.DropDatabaseAsync(dbName);

                try
                {
                    await serverClient.DropUserAsync(dbUser);
                    await serverClient.DropUserAsync(dbUser);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "drop user failed");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "PostgreSQL flexible server test failed");
                throw;
            }
        }
    }
}
