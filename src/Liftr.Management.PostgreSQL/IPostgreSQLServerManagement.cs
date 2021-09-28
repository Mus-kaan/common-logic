//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using System.Threading.Tasks;

namespace Microsoft.Liftr.Management.PostgreSQL
{
    public interface IPostgreSQLServerManagement
    {
        public Task CreateUserIfNotExistAsync(string username, string password);

        public Task UpdatePasswordAsync(string username, string password);

        public Task DropUserAsync(string username);

        public Task CreateDatabaseIfNotExistAsync(string dbName);

        public Task DropDatabaseAsync(string dbName);

        public Task GrantDatabaseAccessAsync(string dbName, string user);
    }
}
