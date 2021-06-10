//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.PostgreSQL.FlexibleServers;
using Microsoft.Azure.Management.PostgreSQL.FlexibleServers.Models;
using Microsoft.Liftr.Fluent;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Management.PostgreSQL
{
    public static class PostgreSQLFlexibleServerExtensions
    {
        public static PostgreSQLManagementClient GetPostgreSQLFlexibleServerClient(this ILiftrAzure liftrAzure)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            var client = new PostgreSQLManagementClient(liftrAzure.AzureCredentials);
            client.SubscriptionId = liftrAzure.DefaultSubscriptionId;

            return client;
        }

        public static async Task<Server> GetPostgreSQLFlexibleServerAsync(this ILiftrAzure liftrAzure, string rgName, string serverName)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = liftrAzure.GetPostgreSQLFlexibleServerClient();
            return await client.Servers.GetAsync(rgName, serverName);
        }

        public static async Task<IEnumerable<Server>> ListPostgreSQLFlexibleServersAsync(this ILiftrAzure liftrAzure, string rgName)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = liftrAzure.GetPostgreSQLFlexibleServerClient();
            return await client.Servers.ListByResourceGroupAsync(rgName);
        }

        public static async Task<Server> CreatePostgreSQLFlexibleServerAsync(this ILiftrAzure liftrAzure, string rgName, string serverName, Server createParameters)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = liftrAzure.GetPostgreSQLFlexibleServerClient();
            return await client.Servers.CreateAsync(rgName, serverName, createParameters);
        }

        public static async Task<Server> UpdatePostgreSQLFlexibleServerAsync(this ILiftrAzure liftrAzure, string rgName, string serverName, ServerForUpdate updateParameters)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = liftrAzure.GetPostgreSQLFlexibleServerClient();
            return await client.Servers.UpdateAsync(rgName, serverName, updateParameters);
        }

        public static async Task<FirewallRule> PostgreSQLFlexibleServerAddIPAsync(this ILiftrAzure liftrAzure, string rgName, string serverName, string ipAddress)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            using var client = liftrAzure.GetPostgreSQLFlexibleServerClient();
            var ruleName = ipAddress.Replace(".", "_");
            var newRule = new FirewallRule(ipAddress, ipAddress);
            return await client.FirewallRules.CreateOrUpdateAsync(rgName, serverName, ruleName, newRule);
        }

        public static async Task PostgreSQLFlexibleServerRemoveIPAsync(this ILiftrAzure liftrAzure, string rgName, string serverName, string ipAddress)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            using var client = liftrAzure.GetPostgreSQLFlexibleServerClient();
            var ruleName = ipAddress.Replace(".", "_");
            try
            {
                await client.FirewallRules.DeleteAsync(rgName, serverName, ruleName);
            }
            catch (Rest.Azure.CloudException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }
        }
    }
}
