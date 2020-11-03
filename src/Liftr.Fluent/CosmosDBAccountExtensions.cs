//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.CosmosDB.Fluent;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr
{
    public static class CosmosDBAccountExtensions
    {
        public static Task<string> GetPrimaryConnectionStringAsync(this ICosmosDBAccount db)
            => GetConnectionStringAsync(db, "Primary MongoDB Connection String");

        public static Task<string> GetSecondaryConnectionStringAsync(this ICosmosDBAccount db)
            => GetConnectionStringAsync(db, "Secondary MongoDB Connection String");

        public static Task<string> GetPrimaryReadOnlyConnectionStringAsync(this ICosmosDBAccount db)
            => GetConnectionStringAsync(db, "Primary Read-Only MongoDB Connection String");

        public static Task<string> GetSecondaryReadOnlyConnectionStringAsync(this ICosmosDBAccount db)
            => GetConnectionStringAsync(db, "Secondary Read-Only MongoDB Connection String");

        public static async Task<string> GetConnectionStringAsync(this ICosmosDBAccount db, string description)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            var dbConnectionStrings = await db.ListConnectionStringsAsync();
            var connStr = dbConnectionStrings.ConnectionStrings.FirstOrDefault(c => c.Description.OrdinalEquals(description)).ConnectionString;
            return connStr;
        }
    }
}
