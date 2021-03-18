//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.PostgreSQL;
using Microsoft.Azure.Management.PostgreSQL.Models;
using Microsoft.Liftr.Fluent;
using System;
using System.Collections.Generic;
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

        public static async Task<Server> GetPostgreSQLServerAsync(this ILiftrAzure liftrAzure, string rgName, string serverName)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = new PostgreSQLManagementClient(liftrAzure.AzureCredentials);
            client.SubscriptionId = liftrAzure.DefaultSubscriptionId;

            return await client.Servers.GetAsync(rgName, serverName);
        }

        public static async Task<IEnumerable<Server>> ListPostgreSQLServersAsync(this ILiftrAzure liftrAzure, string rgName)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = new PostgreSQLManagementClient(liftrAzure.AzureCredentials);
            client.SubscriptionId = liftrAzure.DefaultSubscriptionId;

            return await client.Servers.ListByResourceGroupAsync(rgName);
        }

        public static async Task<Server> CreatePostgreSQLServerAsync(this ILiftrAzure liftrAzure, string rgName, string serverName, ServerForCreate createParameters)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = new PostgreSQLManagementClient(liftrAzure.AzureCredentials);
            client.SubscriptionId = liftrAzure.DefaultSubscriptionId;

            return await client.Servers.CreateAsync(rgName, serverName, createParameters);
        }

        public static async Task<Server> UpdatePostgreSQLServerAsync(this ILiftrAzure liftrAzure, string rgName, string serverName, ServerUpdateParameters updateParameters)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var client = new PostgreSQLManagementClient(liftrAzure.AzureCredentials);
            client.SubscriptionId = liftrAzure.DefaultSubscriptionId;

            return await client.Servers.UpdateAsync(rgName, serverName, updateParameters);
        }
    }
}
