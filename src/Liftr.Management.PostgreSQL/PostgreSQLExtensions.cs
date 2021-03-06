//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.PostgreSQL;
using Microsoft.Azure.Management.PostgreSQL.Models;
using Microsoft.Liftr.Fluent;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Management.PostgreSQL
{
    public static class PostgreSQLExtensions
    {
        public static Task RegisterPostgreSQLRPAsync(this ILiftrAzure liftrAzure)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            return liftrAzure.RegisterResourceProviderAsync("Microsoft.DBforPostgreSQL");
        }

        public static PostgreSQLManagementClient GetPostgreSQLServerClient(this ILiftrAzure liftrAzure)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            var client = new PostgreSQLManagementClient(liftrAzure.AzureCredentials);
            client.SubscriptionId = liftrAzure.DefaultSubscriptionId;

            return client;
        }

        public static async Task<Server> GetPostgreSQLServerAsync(this ILiftrAzure liftrAzure, string rgName, string serverName, CancellationToken cancellationToken = default)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = liftrAzure.GetPostgreSQLServerClient();
            return await client.Servers.GetAsync(rgName, serverName, cancellationToken);
        }

        public static async Task<IEnumerable<Server>> ListPostgreSQLServersAsync(this ILiftrAzure liftrAzure, string rgName, CancellationToken cancellationToken = default)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = liftrAzure.GetPostgreSQLServerClient();
            return await client.Servers.ListByResourceGroupAsync(rgName, cancellationToken);
        }

        public static async Task<Server> CreatePostgreSQLServerAsync(this ILiftrAzure liftrAzure, string rgName, string serverName, ServerForCreate createParameters, CancellationToken cancellationToken = default)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = liftrAzure.GetPostgreSQLServerClient();
            return await client.Servers.CreateAsync(rgName, serverName, createParameters, cancellationToken);
        }

        public static async Task<Server> UpdatePostgreSQLServerAsync(this ILiftrAzure liftrAzure, string rgName, string serverName, ServerUpdateParameters updateParameters, CancellationToken cancellationToken = default)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = liftrAzure.GetPostgreSQLServerClient();
            return await client.Servers.UpdateAsync(rgName, serverName, updateParameters, cancellationToken);
        }

        public static async Task<FirewallRule> PostgreSQLAddIPAsync(this ILiftrAzure liftrAzure, string rgName, string serverName, string ipAddress, CancellationToken cancellationToken = default)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            using var client = liftrAzure.GetPostgreSQLServerClient();
            var ruleName = ipAddress.Replace(".", "_");
            var newRule = new FirewallRule(ipAddress, ipAddress);
            return await client.FirewallRules.CreateOrUpdateAsync(rgName, serverName, ruleName, newRule, cancellationToken);
        }

        public static async Task PostgreSQLRemoveIPAsync(this ILiftrAzure liftrAzure, string rgName, string serverName, string ipAddress, CancellationToken cancellationToken = default)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            using var client = liftrAzure.GetPostgreSQLServerClient();
            var ruleName = ipAddress.Replace(".", "_");
            try
            {
                await client.FirewallRules.DeleteAsync(rgName, serverName, ruleName, cancellationToken);
            }
            catch (Rest.Azure.CloudException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }
        }
    }
}
