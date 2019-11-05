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

        // /subscriptions/60fad35b-3a47-4ca0-b691-4a789f737cea/resourcegroups/unit-test-shared-rg/providers/Microsoft.DocumentDB/databaseAccounts/liftr-unittest-eus-db
        public static string TestMongodbConStr => "bW9uZ29kYjovL2xpZnRyLXVuaXR0ZXN0LWV1cy1kYjpZMTV5UnhTOHo2dlJZNzdNSlJHY2ZRQzJEcXIyV3BIaGpMYTVVN3NES3J6bXpiSWxwNnhTaTdEOUVSNEMxdUxkNjhudUpKQ2xHMWxRSmE3UnNjdUhuQT09QGxpZnRyLXVuaXR0ZXN0LWV1cy1kYi5kb2N1bWVudHMuYXp1cmUuY29tOjEwMjU1Lz9zc2w9dHJ1ZSZyZXBsaWNhU2V0PWdsb2JhbGRi".FromBase64();

        public static IMongoClient TestClient { get; } = new MongoClient(TestMongodbConStr);

        public static IMongoDatabase TestDatabase { get; } = TestClient.GetDatabase(TestDatabaseName);

        public static MongoOptions TestMongoOptions => new MongoOptions() { ConnectionString = TestMongodbConStr, DatabaseName = TestDatabaseName };

        public static string RandomCollectionName() => "test.collection." + ObjectId.GenerateNewId().ToString();
    }
}
