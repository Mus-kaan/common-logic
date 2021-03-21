//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.PostgreSQL.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.Management.PostgreSQL;
using MongoDB.Bson;
using Npgsql;
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

                // Check actual db actions
                {
                    var options = new PostgreSQLOptions()
                    {
                        Server = sqlOptions.Server,
                        ServerResourceName = sqlOptions.ServerResourceName,
                        Username = dbUser,
                        Password = userPassword,
                        Database = dbName,
                    };

                    using var conn = new NpgsqlConnection(options.ConnectionString);
                    await conn.OpenAsync();

                    using (var command = new NpgsqlCommand("DROP TABLE IF EXISTS inventory", conn))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    using (var command = new NpgsqlCommand("CREATE TABLE inventory(id serial PRIMARY KEY, name VARCHAR(50), quantity INTEGER)", conn))
                    {
                        await command.ExecuteNonQueryAsync();
                        Console.Out.WriteLine("Finished creating table");
                    }

                    using (var command = new NpgsqlCommand("INSERT INTO inventory (name, quantity) VALUES (@n1, @q1), (@n2, @q2), (@n3, @q3)", conn))
                    {
                        command.Parameters.AddWithValue("n1", "banana");
                        command.Parameters.AddWithValue("q1", 150);
                        command.Parameters.AddWithValue("n2", "orange");
                        command.Parameters.AddWithValue("q2", 154);
                        command.Parameters.AddWithValue("n3", "apple");
                        command.Parameters.AddWithValue("q3", 100);

                        int nRows = await command.ExecuteNonQueryAsync();
                    }

                    await conn.CloseAsync();
                    await Task.Delay(5000);
                }

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
