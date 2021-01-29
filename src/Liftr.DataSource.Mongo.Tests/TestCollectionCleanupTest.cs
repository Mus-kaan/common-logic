//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo.Tests
{
    public sealed class TestCollectionCleanupTest
    {
        [CheckInValidation(skipLinux: true)]
        public async Task CleanUpOldTestCollectionsAsync()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            var collections = await collectionFactory.ListCollectionsAsync();

            List<Task> taskList = new List<Task>();

            foreach (var collection in collections)
            {
                if (!collection.OrdinalStartsWith(TestDBConnection.TestCollectionPrefix))
                {
                    continue;
                }

                var namePart = collection.Substring(TestDBConnection.TestCollectionPrefix.Length);

                // Keep the new created one for concurrent running tests.
                if (namePart.OrdinalContains("."))
                {
                    var timePart = namePart.Substring(0, namePart.LastIndexOf(".", StringComparison.OrdinalIgnoreCase));
                    if (DateTime.TryParse(timePart, out var parsedTime))
                    {
                        if (parsedTime.ToUniversalTime().AddDays(1) > DateTime.UtcNow)
                        {
                            continue;
                        }
                    }
                }

                taskList.Add(collectionFactory.DeleteCollectionAsync(collection));
            }

            if (taskList.Any())
            {
                await Task.WhenAll(taskList);
            }
        }
    }
}
