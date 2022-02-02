//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Management.PostgreSQL;
using Microsoft.Liftr.Tests;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class PostgreSQLFlexibleServerDevelopment : LiftrTestBase
    {
        public PostgreSQLFlexibleServerDevelopment(ITestOutputHelper output)
            : base(output, useMethodName: true)
        {
        }

        [Fact(Skip = "Used for local development")]
        public async Task VerifyDatabaseCreationAsync()
        {
            var objectId = ObjectId.GenerateNewId().ToString();
            var dbName = "db_grafana_" + objectId;
            var dbUser = "user_grafana_" + objectId;
            var userPassword = Guid.NewGuid().ToString();
            try
            {
                var sqlOptions = new PostgreSQLOptions()
                {
                    Server = "tt-pgsql-flexible-71303.postgres.database.azure.com",
                    ServerResourceName = "tt-pgsql-flexible-71303",
                    Password = string.Empty,
                    Username = "testUser",
                };

                var options = new PostgreSQLServerManagementOptions()
                {
                    ServerAdminUsername = sqlOptions.Username,
                    ConnectionString = sqlOptions.FlexibleServerConnectionString,
                };

                var serverClient = new PostgreSQLServerManagement(options, Logger);

                // All the following management calls are called twice.
                // The code cosumer (RP worker) can be ephemeral, it may restart in the middle of doing some work.
                // Calling the method multiple times should just have no effect without exceptions.
                await serverClient.CreateUserIfNotExistAsync(dbUser, userPassword);
                await serverClient.CreateUserIfNotExistAsync(dbUser, userPassword);

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
                Logger.Error(ex, "PostgreSQL test failed");
                throw;
            }
        }
    }
}
