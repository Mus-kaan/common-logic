//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.Liftr.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr
{
    public static class StorageAccountExtensions
    {
        [Obsolete("We need to handle secret rotation. Please use StorageAccountCredentialLifeCycleManager instead.")]
        public static async Task<string> GetPrimaryConnectionStringAsync(this IStorageAccount storageAccount)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException(nameof(storageAccount));
            }

            var key = await storageAccount.GetPrimaryStorageKeyAsync();

            return key.ToConnectionString(storageAccount.Name);
        }

        [Obsolete("We need to handle secret rotation. Please use StorageAccountCredentialLifeCycleManager instead.")]
        public static async Task<StorageAccountKey> GetPrimaryStorageKeyAsync(this IStorageAccount storageAccount)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException(nameof(storageAccount));
            }

            var key = (await storageAccount.GetKeysAsync()).Where(k => k.KeyName.OrdinalEquals("key1")).FirstOrDefault();

            return key;
        }

        public static string ToConnectionString(this StorageAccountKey key, string accountName)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={key.Value};EndpointSuffix=core.windows.net";
        }

        public static async Task<IStorageAccount> WithAccessFromVNetAsync(this IStorageAccount storageAccount, ISubnet subnet, Serilog.ILogger logger, bool enableVNetFilter = true)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException(nameof(storageAccount));
            }

            if (subnet == null)
            {
                throw new ArgumentNullException(nameof(subnet));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!enableVNetFilter && storageAccount.Inner.NetworkRuleSet.DefaultAction != DefaultAction.Deny)
            {
                logger.Information("Skip adding VNet to storage account with Id '{storageId}' since the Network filter is not enabled.", storageAccount.Id);
                return storageAccount;
            }

            if (enableVNetFilter && storageAccount.Inner.NetworkRuleSet.DefaultAction == DefaultAction.Allow)
            {
                storageAccount = await storageAccount.Update()
                    .WithAccessFromSelectedNetworks()
                    .ApplyAsync();
            }

            logger.Information("Restrict access to storage account with Id '{storageId}' to Subnet '{subnetId}'.", storageAccount.Id, subnet.Inner.Id);
            return await storageAccount.Update()
                .WithAccessFromNetworkSubnet(subnet.Inner.Id)
                .ApplyAsync();
        }

        public static async Task<IStorageAccount> WithAccessFromIpAddressAsync(this IStorageAccount storageAccount, string ip, Serilog.ILogger logger, bool enableVNetFilter = true)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException(nameof(storageAccount));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!enableVNetFilter && storageAccount.Inner.NetworkRuleSet.DefaultAction != DefaultAction.Deny)
            {
                logger.Information("Skip adding IP rules to storage account with Id '{storageId}' since the Network filter is not enabled.", storageAccount.Id);
                return storageAccount;
            }

            if (enableVNetFilter && storageAccount.Inner.NetworkRuleSet.DefaultAction == DefaultAction.Allow)
            {
                storageAccount = await storageAccount.Update()
                    .WithAccessFromSelectedNetworks()
                    .ApplyAsync();
            }

            logger.Information("Restrict access to storage account with Id '{storageId}' to IP '{ip}'.", storageAccount.Id, ip);
            return await storageAccount.Update()
                .WithAccessFromIpAddress(ip)
                .ApplyAsync();
        }

        public static async Task<IStorageAccount> RemoveUnusedVNetRulesAsync(this IStorageAccount storageAccount, ILiftrAzureFactory liftrAzureFactory, Serilog.ILogger logger)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException(nameof(storageAccount));
            }

            if (liftrAzureFactory == null)
            {
                throw new ArgumentNullException(nameof(liftrAzureFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (storageAccount.Inner.NetworkRuleSet.DefaultAction != DefaultAction.Deny)
            {
                logger.Information("Skip removing VNet rules from storage account with Id '{storageId}' since the Network filter is not enabled.", storageAccount.Id);
                return storageAccount;
            }

            List<string> subnetsToRemove = new List<string>();
            foreach (var subnetStr in storageAccount.NetworkSubnetsWithAccess)
            {
                var subnetId = new Liftr.Contracts.ResourceId(subnetStr);
                var az = liftrAzureFactory.GenerateLiftrAzure(subnetId.SubscriptionId);
                var subnet = await az.GetSubnetAsync(subnetStr);
                if (subnet == null)
                {
                    subnetsToRemove.Add(subnetStr);
                }
            }

            if (!subnetsToRemove.Any())
            {
                logger.Information("All subnets in the filter are active.");
                return storageAccount;
            }

            var update = storageAccount.Update();

            foreach (var subnet in subnetsToRemove)
            {
                logger.Information("Removing subnet '{subnetId}' from VNet filter of storage '{storageName}' ...", subnet, storageAccount.Name);
                update = update.WithoutNetworkSubnetAccess(subnet);
            }

            return await update.ApplyAsync();
        }

        public static async Task<IStorageAccount> TurnOffVNetAsync(this IStorageAccount storageAccount, Serilog.ILogger logger)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException(nameof(storageAccount));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (storageAccount.Inner?.NetworkRuleSet?.DefaultAction == DefaultAction.Allow)
            {
                logger.Information("Skip turning off VNet of storage account with Id '{storageId}' since the Network filter is not enabled.", storageAccount.Id);
                return storageAccount;
            }

            logger.Information("Turning off VNet of storage account with Id '{storageId}'.", storageAccount.Id);
            return await storageAccount.Update().WithAccessFromAllNetworks().ApplyAsync();
        }
    }
}
