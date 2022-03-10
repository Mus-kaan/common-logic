//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo;
using System;

namespace Microsoft.Liftr.VNetInjection.DataSource.Mongo
{
    public class VNetInjectionMongoOptions : MongoOptions
    {
        public string VNetInjectionCollectionName { get; set; } = "vnet-injection";

        public void CheckValid()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new InvalidOperationException("ConnectionString should not be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(DatabaseName))
            {
                throw new InvalidOperationException("DatabaseName should not be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(VNetInjectionCollectionName))
            {
                throw new InvalidOperationException($"{nameof(VNetInjectionCollectionName)} should not be null or empty.");
            }
        }
    }
}
