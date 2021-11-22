//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo;
using System;

namespace Microsoft.Liftr.ManagedIdentity.DataSource
{
    public class ManagedIdentityMongoOptions : MongoOptions
    {
        public string ManagedIdentityCollectionName { get; set; } = "managed-identities";

        public void CheckValid()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new InvalidOperationException($"{nameof(ConnectionString)} should not be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(DatabaseName))
            {
                throw new InvalidOperationException($"{nameof(DatabaseName)} should not be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(ManagedIdentityCollectionName))
            {
                throw new InvalidOperationException($"{nameof(ManagedIdentityCollectionName)} should not be null or empty.");
            }
        }
    }
}
