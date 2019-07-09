//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Diagnostics;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public sealed class TestCollectionScope<T> : IDisposable
    {
        private readonly IMongoDatabase _db;
        private readonly string _collectionName;

        public TestCollectionScope(Func<IMongoDatabase, string, IMongoCollection<T>> collectionCrator)
        {
            _db = TestDBConnection.TestDatabase;
            _collectionName = GenerateRandomCollectionName();
            Collection = collectionCrator(_db, _collectionName);
        }

        public IMongoCollection<T> Collection { get; }

        public void Dispose()
        {
            try
            {
                _db.DropCollection(_collectionName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        #region Private methods
        private static string GenerateRandomCollectionName()
        {
            return "test.collection." + ObjectId.GenerateNewId().ToString();
        }
        #endregion
    }
}
