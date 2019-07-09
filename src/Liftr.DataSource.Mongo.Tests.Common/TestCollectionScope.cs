//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

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
            : this(TestDBConnection.TestDatabase, TestDBConnection.RandomCollectionName(), collectionCrator)
        {
        }

        public TestCollectionScope(IMongoDatabase db, Func<IMongoDatabase, string, IMongoCollection<T>> collectionCrator)
            : this(db, TestDBConnection.RandomCollectionName(), collectionCrator)
        {
        }

        public TestCollectionScope(IMongoDatabase db, string collectionName, Func<IMongoDatabase, string, IMongoCollection<T>> collectionCrator)
        {
            _db = db;
            _collectionName = collectionName;
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
    }
}
