//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Npgsql;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Management.PostgreSQL
{
    public class PostgreSQLServerManagement : IPostgreSQLServerManagement
    {
        private readonly PostgreSQLServerManagementOptions _sqlOptions;
        private readonly Serilog.ILogger _logger;

        public PostgreSQLServerManagement(PostgreSQLServerManagementOptions sqlOptions, Serilog.ILogger logger)
        {
            _sqlOptions = sqlOptions ?? throw new ArgumentNullException(nameof(sqlOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CreateUserIfNotExistAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            using var dbConnection = new NpgsqlConnection(_sqlOptions.ConnectionString);
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var createCommand = new NpgsqlCommand($"CREATE ROLE {username} WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION CONNECTION LIMIT 10 PASSWORD '{password}'", dbConnection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            await dbConnection.OpenAsync();
            _logger.Information("Start creating DB user {username}", username);
            try
            {
                await createCommand.ExecuteNonQueryAsync();
            }
            catch (PostgresException ex) when (ex.SqlState.OrdinalEquals("42710"))
            {
                _logger.Warning(ex, "The DB user already exist. Skip.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Create user failed");
                throw;
            }
        }

        public async Task UpdatePasswordAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            using var dbConnection = new NpgsqlConnection(_sqlOptions.ConnectionString);
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var createCommand = new NpgsqlCommand($"ALTER USER {username} WITH PASSWORD '{password}'", dbConnection);
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities

            await dbConnection.OpenAsync();
            _logger.Information("Start updating password for DB user {username}", username);
            try
            {
                await createCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Update password for user failed");
                throw;
            }
        }

        public async Task DropUserAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            using var dbConnection = new NpgsqlConnection(_sqlOptions.ConnectionString);
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var createCommand = new NpgsqlCommand($"DROP ROLE IF EXISTS {username}", dbConnection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            await dbConnection.OpenAsync();
            _logger.Information("Start executing DB command: {dbCommand}", createCommand.CommandText);
            await createCommand.ExecuteNonQueryAsync();
        }

        public async Task CreateDatabaseIfNotExistAsync(string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentNullException(nameof(dbName));
            }

            var owner = _sqlOptions.ServerAdminUsername;

            using var dbConnection = new NpgsqlConnection(_sqlOptions.ConnectionString);
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var createCommand = new NpgsqlCommand($"CREATE DATABASE {dbName} WITH OWNER = \"{owner}\" ENCODING = 'UTF8' CONNECTION LIMIT = -1", dbConnection);
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

        public async Task DropDatabaseAsync(string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentNullException(nameof(dbName));
            }

            using var dbConnection = new NpgsqlConnection(_sqlOptions.ConnectionString);
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var createCommand = new NpgsqlCommand($"DROP DATABASE IF EXISTS {dbName}", dbConnection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            await dbConnection.OpenAsync();
            _logger.Information("Start executing DB command: {dbCommand}", createCommand.CommandText);
            await createCommand.ExecuteNonQueryAsync();
        }

        public async Task GrantDatabaseAccessAsync(string dbName, string user)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentNullException(nameof(dbName));
            }

            if (string.IsNullOrEmpty(user))
            {
                throw new ArgumentNullException(nameof(user));
            }

            var owner = _sqlOptions.ServerAdminUsername;

            using var dbConnection = new NpgsqlConnection(_sqlOptions.ConnectionString);
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var createCommand = new NpgsqlCommand($"GRANT ALL ON DATABASE {dbName} TO {user}", dbConnection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            await dbConnection.OpenAsync();
            _logger.Information("Start executing DB command: {dbCommand}", createCommand.CommandText);
            await createCommand.ExecuteNonQueryAsync();
        }
    }
}
