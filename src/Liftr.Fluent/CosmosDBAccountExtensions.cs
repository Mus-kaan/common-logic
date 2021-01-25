//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent.Models;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Liftr.Fluent;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr
{
    public static class CosmosDBAccountExtensions
    {
        private const string c_apiVersion = "2020-04-01";
        private const string c_turnOffVNetPATCHBody = "{\"properties\":{\"ipRules\":[],\"isVirtualNetworkFilterEnabled\":false,\"virtualNetworkRules\":[]}}";

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

        public static async Task<ICosmosDBAccount> TurnOffVNetAsync(this ICosmosDBAccount db, ILiftrAzure liftrAzure)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            db = await db.RefreshAsync();
            if (!db.VirtualNetoworkFilterEnabled)
            {
                return db;
            }

            await liftrAzure.PatchResourceAsync(db.Id, c_apiVersion, c_turnOffVNetPATCHBody);
            return await db.WaitForUpdatingAsync();
        }

        public static async Task<ICosmosDBAccount> TurnOnVNetAsync(this ICosmosDBAccount db, IList<VirtualNetworkRule> virtualNetworkRules = null)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (db.VirtualNetoworkFilterEnabled)
            {
                return db;
            }

            db = await db.RefreshAsync();
            var updatable = db.Update().WithVirtualNetworkFilterEnabled(true);
            if (virtualNetworkRules != null)
            {
                updatable = updatable.WithVirtualNetworkRules(virtualNetworkRules);
            }

            db = await updatable.ApplyAsync();
            return await db.WaitForUpdatingAsync();
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

            db = await db.WaitForUpdatingAsync();

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

        public static async Task<IDisposable> StartOpenNetworkScopeAsync(this ICosmosDBAccount db, ILiftrAzure liftrAzure, Serilog.ILogger logger)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            db = await db.RefreshAsync();

            if (db.VirtualNetoworkFilterEnabled)
            {
                logger.Information("Open the database to all Networks temporarily. This will switched back to restricted Network. db: '{dbId}'", db.Id);
                var vnetRules = db.VirtualNetworkRules.ToList();
                db = await db.TurnOffVNetAsync(liftrAzure);
                logger.Information("Wait for 15 minutes to make sure the VNet rules are synced.");
                await Task.Delay(TimeSpan.FromMinutes(15));
                return new CosmosDBOpenNetworkScope(db, logger, vnetRules);
            }

            return new CosmosDBOpenNetworkScope(db, logger);
        }

        public static async Task<ICosmosDBAccount> WaitForUpdatingAsync(this ICosmosDBAccount db)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            db = await db.RefreshAsync();

            while (db.Inner.ProvisioningState.OrdinalEquals("Updating"))
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                db = await db.RefreshAsync();
            }

            return db;
        }
    }

    internal sealed class CosmosDBOpenNetworkScope : IDisposable
    {
        private readonly ICosmosDBAccount _db;
        private readonly ILogger _logger;
        private readonly IList<VirtualNetworkRule> _vnetRulesToRestore;

        public CosmosDBOpenNetworkScope(ICosmosDBAccount db, Serilog.ILogger logger, IList<VirtualNetworkRule> vnetRulesToRestore = null)
        {
            _db = db;
            _logger = logger;
            _vnetRulesToRestore = vnetRulesToRestore;
        }

        public void Dispose()
        {
            if (_vnetRulesToRestore != null)
            {
                _logger.Information("Turn VNet restrictions back on for db: '{dbId}'", _db.Id);
                _db.TurnOnVNetAsync(_vnetRulesToRestore).Wait();
            }
        }
    }
}
