//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.PostgreSQL.FlexibleServers;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Management.PostgreSQL;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using Microsoft.Liftr.Utilities;
using MongoDB.Bson;
using Npgsql;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class PostgreSQLFlexibleServerUserManagementTests : LiftrAzureTestBase
    {
        public PostgreSQLFlexibleServerUserManagementTests(ITestOutputHelper output)
            : base(output, useMethodName: true)
        {
        }

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS2]
        public async Task VerifyPostgreSQLUserSecretRotationAsync()
        {
            try
            {
                var azure = Client;
                var name = SdkContext.RandomResourceName("tt-pgsql-user-rotate-", 15);

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

                var listResult = await azure.ListPostgreSQLFlexibleServersAsync(ResourceGroupName);
                Assert.Single(listResult);

                var getResult = await azure.GetPostgreSQLFlexibleServerAsync(ResourceGroupName, name);
                Assert.Equal(name, getResult.Name);

                var objectId = ObjectId.GenerateNewId().ToString();
                var dbName = "db_grafana_" + objectId;
                var rolename = "role_grafana_" + objectId;
                var dbUser1 = "user_grafana_" + objectId;
                var userPassword1 = Guid.NewGuid().ToString();
                var dbUser2 = "secondary_user_grafana_" + objectId;
                var userPassword2 = Guid.NewGuid().ToString();

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
                    await serverClient.CreateUsersIfNotExistAsync(rolename, dbUser1, userPassword1, dbUser2, userPassword2);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "add users failed");
                    throw;
                }

                // All the following management calls are called twice.
                // The code cosumer (RP worker) can be ephemeral, it may restart in the middle of doing some work.
                // Calling the method multiple times should just have no effect without exceptions.
                await serverClient.CreateUsersIfNotExistAsync(rolename, dbUser1, userPassword1, dbUser2, userPassword2);

                await Task.Delay(TimeSpan.FromMinutes(2));

                await serverClient.CreateDatabaseIfNotExistAsync(dbName);
                await serverClient.CreateDatabaseIfNotExistAsync(dbName);

                await serverClient.GrantDatabaseAccessAsync(dbName, rolename);
                await serverClient.GrantDatabaseAccessAsync(dbName, rolename);

                var tableName = "test_table_" + objectId;

                Logger.Information($"Verify user {dbUser1} can read and write db.");
                await VerifyCanReadWriteDBAsync(sqlOptions.Server, dbUser1, userPassword1, dbName, tableName);

                Logger.Information($"Verify user {dbUser2} can read and write db.");
                await VerifyCanReadWriteDBAsync(sqlOptions.Server, dbUser2, userPassword2, dbName, tableName);

                Logger.Information($"Change user {dbUser1} password and verify it still can read and write db.");
                var newUserPassword1 = PasswordGenerator.Generate(length: 36, includeSpecialCharacter: false);
                await serverClient.UpdatePasswordAsync(dbUser1, newUserPassword1);
                await VerifyCanReadWriteDBAsync(sqlOptions.Server, dbUser1, newUserPassword1, dbName, tableName);

                Logger.Information($"Change user {dbUser2} password and verify it still can read and write db.");
                var newUserPassword2 = PasswordGenerator.Generate(length: 36, includeSpecialCharacter: false);
                await serverClient.UpdatePasswordAsync(dbUser2, newUserPassword2);
                await VerifyCanReadWriteDBAsync(sqlOptions.Server, dbUser2, newUserPassword2, dbName, tableName);

                await serverClient.DropDatabaseAsync(dbName);
                await serverClient.DropDatabaseAsync(dbName);

                try
                {
                    await serverClient.DropUsersAsync(rolename, dbUser1, dbUser2);
                    await serverClient.DropUsersAsync(rolename, dbUser1, dbUser2);
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

        private async Task VerifyCanReadWriteDBAsync(string serverName, string dbUser, string userPassword, string dbName, string tableName)
        {
            var userConnectionString = $"Server={serverName};Username={dbUser};Database={dbName};Port=5432;Password={userPassword};SSLMode=Require";

            using var dbConnection = new NpgsqlConnection(userConnectionString);
            await dbConnection.OpenAsync();

            // create table if not exist
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var createCommand = new NpgsqlCommand($"CREATE TABLE IF NOT EXISTS {tableName} (username VARCHAR(50) NOT NULL)", dbConnection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            Logger.Information("Start executing DB command: {dbCommand}", createCommand.CommandText);
            await createCommand.ExecuteNonQueryAsync();

            // write to existing table
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var writeCommand = new NpgsqlCommand($"INSERT INTO {tableName} (username) VALUES ('{dbUser}')", dbConnection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            Logger.Information("Start executing DB command: {dbCommand}", writeCommand.CommandText);
            await writeCommand.ExecuteNonQueryAsync();

            // read existing table
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var readCommand = new NpgsqlCommand($"SELECT * FROM {tableName}", dbConnection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            Logger.Information("Start executing DB command: {dbCommand}", readCommand.CommandText);
            await readCommand.ExecuteNonQueryAsync();
        }
    }
}
