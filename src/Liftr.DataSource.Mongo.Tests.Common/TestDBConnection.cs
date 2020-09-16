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

        // /subscriptions/f885cf14-b751-43c1-9536-dc5b1be02bc0/resourceGroups/liftr-unittest-infra/providers/Microsoft.DocumentDb/databaseAccounts/liftr-unittest-wus-db (Microsoft tenant)
        public static string TestMongodbConStr => "bW9uZ29kYjovL2xpZnRyLXVuaXR0ZXN0LXd1cy1kYjo4RGlmNHF3TDY2SjJlYWZCWEZGWDNzdU9UUmNNNlRGSklNQnNWeG5DTmFQS05xMlBiSTBmdEdnQkg5RUhXRlJsTlVWZW10elJuR1p4b2hLaVFlUVRpdz09QGxpZnRyLXVuaXR0ZXN0LXd1cy1kYi5kb2N1bWVudHMuYXp1cmUuY29tOjEwMjU1Lz9zc2w9dHJ1ZSZyZXBsaWNhU2V0PWdsb2JhbGRi".FromBase64();

        public static IMongoClient TestClient { get; } = new MongoClient(TestMongodbConStr);

        public static IMongoDatabase TestDatabase { get; } = TestClient.GetDatabase(TestDatabaseName);

        public static MongoOptions TestMongoOptions => new MongoOptions() { ConnectionString = TestMongodbConStr, DatabaseName = TestDatabaseName };

        public static string RandomCollectionName() => "test.collection." + ObjectId.GenerateNewId().ToString();
    }
}
