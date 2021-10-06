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
using Npgsql;
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

                // user (aka role) of postgresql has to be lower-case, because for some reason the cmd "GRANT {role} to {user}" is converting the {user} to lower case, then add to table "pg_auth_members" under azure_sys/Catalogs/PostgreSQL_Catalog(pg_catalog). If the {user} is not lower case, it will throw "user(role) not found for lowerCaseOf{user}"
                var adminUser = "test_user";
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

                try
                {
                    await serverClient.UpdatePasswordAsync(dbUser, userPassword);
                    await serverClient.UpdatePasswordAsync(dbUser, userPassword);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "update password failed");
                    throw;
                }

                await serverClient.CreateDatabaseIfNotExistAsync(dbName);
                await serverClient.CreateDatabaseIfNotExistAsync(dbName);

                await serverClient.GrantDatabaseAccessAsync(dbName, dbUser);
                await serverClient.GrantDatabaseAccessAsync(dbName, dbUser);

                await TestKillProcessRelatedDBAsync(serverClient, sqlOptions.Server, dbUser, userPassword, dbName, options);

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

        private async Task TestKillProcessRelatedDBAsync(PostgreSQLServerManagement serverClient, string serverName, string dbUser, string userPassword, string dbName, PostgreSQLServerManagementOptions options)
        {
            // Create 2 additional user connections to the database
            var userConnectionString = $"Server={serverName};Username={dbUser};Database={dbName};Port=5432;Password={userPassword};SSLMode=Require";

            using var userConn1 = new NpgsqlConnection(userConnectionString);
            using var userConn2 = new NpgsqlConnection(userConnectionString);
            await userConn1.OpenAsync();
            await userConn2.OpenAsync();

            var countBefore = await CountActiveRecordAsync(options, dbName);
            Assert.Equal(2, countBefore);

            await serverClient.KillProcessRelatedToDatabaseAsync(dbName);

            var countAfter = await CountActiveRecordAsync(options, dbName);
            Assert.Equal(0, countAfter);
        }

        private static async Task<long> CountActiveRecordAsync(PostgreSQLServerManagementOptions options, string dbName)
        {
            using var superConn = new NpgsqlConnection(options.ConnectionString);
            await superConn.OpenAsync();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var countCMD = new NpgsqlCommand($"SELECT COUNT(*) FROM pg_stat_activity WHERE pg_stat_activity.datname = '{dbName}' AND pid <> pg_backend_pid()", superConn);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            var res = await countCMD.ExecuteScalarAsync();
            return (long)res;
        }
    }
}
