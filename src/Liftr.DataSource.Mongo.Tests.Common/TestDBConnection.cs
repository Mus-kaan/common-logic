//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Driver;
using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public static class TestDBConnection
    {
        public const string TestDatabaseName = "unit-test";
        public const string TestCollectionPrefix = "test.collection.";

        private const string LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64 = nameof(LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64);

        public static string TestMongodbConStr
        {
            get
            {
                var encodedConnStr = Environment.GetEnvironmentVariable(LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64);
                if (string.IsNullOrEmpty(encodedConnStr))
                {
                    throw new InvalidOperationException($"Cannot find the credential for running the unit tests. It should be set in the environment variable with name {LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64}. Details: https://aka.ms/liftr-test-cred");
                }

                return encodedConnStr.FromBase64();
            }
        }

        public static IMongoClient TestClient { get; } = new MongoClient(TestMongodbConStr);

        public static IMongoDatabase TestDatabase { get; } = TestClient.GetDatabase(TestDatabaseName);

        public static MongoOptions TestMongoOptions => new MongoOptions() { ConnectionString = TestMongodbConStr, DatabaseName = TestDatabaseName };

        public static string RandomCollectionName() => $"{TestCollectionPrefix}{DateTime.UtcNow.ToZuluString()}.{ObjectId.GenerateNewId()}";
    }
}
