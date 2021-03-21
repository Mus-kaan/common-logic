//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Npgsql;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Management.PostgreSQL
{
    public class PostgreSQLServerManagement
    {
        private readonly PostgreSQLOptions _adminOptions;
        private readonly Serilog.ILogger _logger;

        public PostgreSQLServerManagement(PostgreSQLOptions adminOptions, Serilog.ILogger logger)
        {
            _adminOptions = adminOptions ?? throw new ArgumentNullException(nameof(adminOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CreateDatabaseIfNotExistAsync(string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentNullException(nameof(dbName));
            }

            using var dbConnection = new NpgsqlConnection(_adminOptions.ConnectionString);
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var createCommand = new NpgsqlCommand($"CREATE DATABASE {dbName} WITH OWNER = {_adminOptions.Username} ENCODING = 'UTF8' CONNECTION LIMIT = -1", dbConnection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            await dbConnection.OpenAsync();
            _logger.Information("Start executing DB command: {dbCommand}", createCommand.CommandText);
            try
            {
                await createCommand.ExecuteNonQueryAsync();
            }
            catch (PostgresException ex) when (ex.SqlState.OrdinalEquals("42P04"))
            {
                _logger.Warning(ex, $"Database with name already exist. Skip.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Create database failed");
                throw;
            }
        }

        public async Task DropDatabaseIfNotExistAsync(string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentNullException(nameof(dbName));
            }

            using var dbConnection = new NpgsqlConnection(_adminOptions.ConnectionString);
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var createCommand = new NpgsqlCommand($"DROP DATABASE IF EXISTS {dbName}", dbConnection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            await dbConnection.OpenAsync();
            _logger.Information("Start executing DB command: {dbCommand}", createCommand.CommandText);
            await createCommand.ExecuteNonQueryAsync();
        }
    }
}
