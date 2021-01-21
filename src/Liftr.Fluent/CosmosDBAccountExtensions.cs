//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Serilog;
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

        public static async Task<ICosmosDBAccount> WithVirtualNetworkRuleAsync(this ICosmosDBAccount db, ISubnet subnet, Serilog.ILogger logger, bool enableVNetFilter = true)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (subnet == null)
            {
                throw new ArgumentNullException(nameof(subnet));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!enableVNetFilter && !db.VirtualNetoworkFilterEnabled)
            {
                logger.Information("Skip adding VNet rules to cosmos DB with Id '{cosmosDBId}' since the VNet filter is not enabled.", db.Id);
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

        public static async Task<IDisposable> StartOpenNetworkScopeAsync(this ICosmosDBAccount db, Serilog.ILogger logger)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (db.VirtualNetoworkFilterEnabled)
            {
                logger.Information("Open the database to all Networks temporarily. This will switched back to restricted Network. db: '{dbId}'", db.Id);
                db = await db.Update().WithVirtualNetworkFilterEnabled(false).ApplyAsync();
                return new CosmosDBOpenNetworkScope(db, logger, enableVNet: true);
            }

            return new CosmosDBOpenNetworkScope(db, logger, enableVNet: false);
        }
    }

    internal sealed class CosmosDBOpenNetworkScope : IDisposable
    {
        private readonly ICosmosDBAccount _db;
        private readonly ILogger _logger;
        private readonly bool _enableVNet;

        public CosmosDBOpenNetworkScope(ICosmosDBAccount db, Serilog.ILogger logger, bool enableVNet)
        {
            _db = db;
            _logger = logger;
            _enableVNet = enableVNet;
        }

        public void Dispose()
        {
            if (_enableVNet)
            {
                _logger.Information("Tuen VNet restrictions back on for db: '{dbId}'", _db.Id);
                _db.Update().WithVirtualNetworkFilterEnabled(true).Apply();
            }
        }
    }
}
