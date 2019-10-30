//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Storage.Fluent;
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

            var key = (await storageAccount.GetKeysAsync()).Where(k => k.KeyName.OrdinalEquals("key1")).FirstOrDefault();

            return $"DefaultEndpointsProtocol=https;AccountName={storageAccount.Name};AccountKey={key.Value};EndpointSuffix=core.windows.net";
        }
    }
}
