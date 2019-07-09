//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;

namespace Microsoft.Liftr.DataSource.Mongo
{
    internal static class TestDBConnection
    {
        // TODO: remove this hard-code testing cosmos db.
        public static string TestMongodbConStr = "bW9uZ29kYjovL25naW54LWRldjpvdHpXa0FIV1NzMGZzVTFnbFVCRkEwS3ZQZXllM1hiMmZ1RHhwUmpDcUp1QWdTbHJOclF3cXdwY2hlRlhLdUt5cmIzZzlTTGVMM0V2MkpsYjE5N25Edz09QG5naW54LWRldi5kb2N1bWVudHMuYXp1cmUuY29tOjEwMjU1Lz9zc2w9dHJ1ZSZyZXBsaWNhU2V0PWdsb2JhbGRi".FromBase64();

        private const string MongodbDatabaseName = "liftr-unit-test";

        public static IMongoClient TestClient { get; } = new MongoClient(TestMongodbConStr);

        public static IMongoDatabase TestDatabase { get; } = TestClient.GetDatabase(MongodbDatabaseName);

        public static MongoOptions TestMongoOptions => new MongoOptions() { ConnectionString = TestMongodbConStr, DatabaseName = MongodbDatabaseName };
    }
}
