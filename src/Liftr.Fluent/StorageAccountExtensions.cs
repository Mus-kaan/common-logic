//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr
{
    public static class StorageAccountExtensions
    {
        public static async Task<string> GetPrimaryConnectionStringAsync(this IStorageAccount storageAccount)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException(nameof(storageAccount));
            }

            var key = await storageAccount.GetPrimaryStorageKeyAsync();

            return key.ToConnectionString(storageAccount.Name);
        }

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

            if (storageAccount.Inner.NetworkRuleSet.DefaultAction == DefaultAction.Allow)
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
    }
}
