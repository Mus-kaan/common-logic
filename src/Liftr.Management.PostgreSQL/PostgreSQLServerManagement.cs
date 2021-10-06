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
            try
            {
                await DoDropDatabaseAsync(dbName);
            }
            catch (PostgresException ex) when (ex.SqlState.OrdinalEquals("55006"))
            {
                // "{databaseName}" is being accessed by other users
                _logger.Warning(ex, $"Try to kill the process related to {dbName}, and retry dropping database");
                await KillProcessRelatedToDatabaseAsync(dbName);
                await DoDropDatabaseAsync(dbName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Drop database failed");
                throw;
            }
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

        // Query pg_stat_activity and get the pid values you want to kill, then issue SELECT pg_terminate_backend(pid int) to them.
        // References:
        // https://stackoverflow.com/questions/5408156/how-to-drop-a-postgresql-database-if-there-are-active-connections-to-it
        // https://www.leeladharan.com/drop-a-postgresql-database-if-there-are-active-connections-to-it/
        public async Task KillProcessRelatedToDatabaseAsync(string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentNullException(nameof(dbName));
            }

            using var dbAdminConnection = new NpgsqlConnection(_sqlOptions.ConnectionString);
            await dbAdminConnection.OpenAsync();

            // Need to grant the admin user role "pg_signal_backend" to kill other process
            // If it has been granted previously, it will skip without changing postgresql database
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var grantCmd = new NpgsqlCommand($"GRANT pg_signal_backend TO {_sqlOptions.ServerAdminUsername}", dbAdminConnection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            _logger.Information("Start executing grant pg_signal_backend command: {dbCommand}", grantCmd.CommandText);
            await grantCmd.ExecuteNonQueryAsync();

            // Kill processes related to the targeting database
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using var killProcessCommand = new NpgsqlCommand($"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '{dbName}' AND pid <> pg_backend_pid()", dbAdminConnection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            _logger.Information("Start executing DB command: {dbCommand}", killProcessCommand.CommandText);
            await killProcessCommand.ExecuteNonQueryAsync();
        }

        private async Task DoDropDatabaseAsync(string dbName)
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
    }
}
