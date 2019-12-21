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
    }
}
