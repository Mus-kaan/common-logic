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
        // AME: public static string TestMongodbConStr => "bW9uZ29kYjovL25vLWRlbGV0ZS1saWZ0ci11bml0LXRlc3QtZGI6bEl0YTlEM1ZETkZEM1lVaEFzdENnQ3JKNTRTWDc2Tm9JdmI4alFncTFDNkhROVlicTFua001bURPOGg3UVJkMWdMdHZsQUx6QTBWSFJuM1FSRDVWUkE9PUBuby1kZWxldGUtbGlmdHItdW5pdC10ZXN0LWRiLmRvY3VtZW50cy5henVyZS5jb206MTAyNTUvP3NzbD10cnVlJnJlcGxpY2FTZXQ9Z2xvYmFsZGI=".FromBase64();
        public static string TestMongodbConStr => "bW9uZ29kYjovL3VuaXR0ZXN0LWRiOmhDclk5anlubEVkUnlPcVFweU16bzRNbWZyRklENmk1M05tM2lNVmM5dlNtWjBCQkE5NmluRmlUY1JOaDZlWGRMYWRXcHJGUVpCTTR4RnhMcDdPUjh3PT1AdW5pdHRlc3QtZGIuZG9jdW1lbnRzLmF6dXJlLmNvbToxMDI1NS8/c3NsPXRydWUmcmVwbGljYVNldD1nbG9iYWxkYg==".FromBase64();

        public static IMongoClient TestClient { get; } = new MongoClient(TestMongodbConStr);

        public static IMongoDatabase TestDatabase { get; } = TestClient.GetDatabase(TestDatabaseName);

        public static MongoOptions TestMongoOptions => new MongoOptions() { ConnectionString = TestMongodbConStr, DatabaseName = TestDatabaseName };

        public static string RandomCollectionName() => "test.collection." + ObjectId.GenerateNewId().ToString();
    }
}
