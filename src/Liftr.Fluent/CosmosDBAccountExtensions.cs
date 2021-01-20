//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
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

        public static async Task<ICosmosDBAccount> WithVirtualNetworkRuleAsync(this ICosmosDBAccount db, ISubnet subnet, Serilog.ILogger logger = null, bool enableVNetFilter = true)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (subnet == null)
            {
                throw new ArgumentNullException(nameof(subnet));
            }

            if (!enableVNetFilter && !db.VirtualNetoworkFilterEnabled)
            {
                if (logger != null)
                {
                    logger.Information("Skip adding VNet rules to cosmos DB with Id '{cosmosDBId}' since the VNet filter is not enabled.", db.Id);
                }

                return db;
            }

            // The cosmos DB service endpoint PUT is not idempotent. PUT the same subnet Id will generate 400.
            var dbVNetRules = db.VirtualNetworkRules;
            if (dbVNetRules?.Any((subnetId) => subnetId?.Id?.OrdinalEquals(subnet.Inner.Id) == true) != true)
            {
                if (logger != null)
                {
                    logger.Information("Restrict access to cosmos DB with Id '{cosmosDBId}' to subnet '{subnetId}'.", db.Id, subnet.Inner.Id);
                }

                return await db.Update().WithVirtualNetworkRule(subnet.Parent.Id, subnet.Name).ApplyAsync();
            }

            return db;
        }
    }
}
