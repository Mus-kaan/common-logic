//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public static class TestDBConnection
    {
        public const string TestDatabaseName = "unit-test";

        // TODO: remove this hard-code testing cosmos db.
        public static string TestMongodbConStr => "bW9uZ29kYjovL25naW54LWRldjpvdHpXa0FIV1NzMGZzVTFnbFVCRkEwS3ZQZXllM1hiMmZ1RHhwUmpDcUp1QWdTbHJOclF3cXdwY2hlRlhLdUt5cmIzZzlTTGVMM0V2MkpsYjE5N25Edz09QG5naW54LWRldi5kb2N1bWVudHMuYXp1cmUuY29tOjEwMjU1Lz9zc2w9dHJ1ZSZyZXBsaWNhU2V0PWdsb2JhbGRi".FromBase64();

        public static IMongoClient TestClient { get; } = new MongoClient(TestMongodbConStr);

        public static IMongoDatabase TestDatabase { get; } = TestClient.GetDatabase(TestDatabaseName);

        public static MongoOptions TestMongoOptions => new MongoOptions() { ConnectionString = TestMongodbConStr, DatabaseName = TestDatabaseName };

        public static string RandomCollectionName() => "test.collection." + ObjectId.GenerateNewId().ToString();
    }
}
