//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.EventHubManager.Tests
{
    public class EventHubManagerSimpleTests
    {
        public EventHubManagerSimpleTests()
        {
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public void EventHubManagerGetAll()
        {
            var logger = Log.Logger;

            var testData = GenerateTestData(MonitoringResourceProvider.Datadog, "westus", 5);
            var ehDataSource = new Mock<IEventHubEntityDataSource>();
            ehDataSource.Setup(s => s.ListAsync(It.IsAny<MonitoringResourceProvider>())).Returns(Task.FromResult(testData));

            var ehManager = new EventHubManagerSimple(ehDataSource.Object, MonitoringResourceProvider.Datadog, logger);

            var items = ehManager.GetAll("westus");

            // validation
            Assert.NotNull(items);
            Assert.Equal(5, items.Count);
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public void EventHubManagerGetAllExcludesInactive()
        {
            var logger = Log.Logger;

            var testData = GenerateTestData(MonitoringResourceProvider.Datadog, "westus", 5);
            var testDataLst = testData.ToList();
            testDataLst[0].Active = false;
            testDataLst[1].Active = false;
            var ehDataSource = new Mock<IEventHubEntityDataSource>();
            ehDataSource.Setup(s => s.ListAsync(It.IsAny<MonitoringResourceProvider>())).Returns(Task.FromResult(testDataLst.AsEnumerable()));

            var ehManager = new EventHubManagerSimple(ehDataSource.Object, MonitoringResourceProvider.Datadog, logger);

            var items = ehManager.GetAll("westus");

            // validation
            Assert.NotNull(items);
            Assert.Equal(3, items.Count);
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public void EventHubManagerGetAllExcludesIngestionDisabled()
        {
            var logger = Log.Logger;

            var testData = GenerateTestData(MonitoringResourceProvider.Datadog, "westus", 5);
            var testDataLst = testData.ToList();
            testDataLst[0].IngestionEnabled = false;
            testDataLst[1].IngestionEnabled = false;
            var ehDataSource = new Mock<IEventHubEntityDataSource>();
            ehDataSource.Setup(s => s.ListAsync(It.IsAny<MonitoringResourceProvider>())).Returns(Task.FromResult(testDataLst.AsEnumerable()));

            var ehManager = new EventHubManagerSimple(ehDataSource.Object, MonitoringResourceProvider.Datadog, logger);

            var items = ehManager.GetAll("westus");

            // validation
            Assert.NotNull(items);
            Assert.Equal(3, items.Count);
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public void EventHubManagerInvalidRegionGetAll()
        {
            var logger = Log.Logger;

            var testData = GenerateTestData(MonitoringResourceProvider.Datadog, "westus", 5);
            var ehDataSource = new Mock<IEventHubEntityDataSource>();
            ehDataSource.Setup(s => s.ListAsync(It.IsAny<MonitoringResourceProvider>())).Returns(Task.FromResult(testData));

            var ehManager = new EventHubManagerSimple(ehDataSource.Object, MonitoringResourceProvider.Datadog, logger);

            var items = ehManager.GetAll("westus2");

            // validation
            Assert.Null(items);
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public void EventHubManagerGet()
        {
            var logger = Log.Logger;

            var testData = GenerateTestData(MonitoringResourceProvider.Datadog, "westus", 5);
            var ehDataSource = new Mock<IEventHubEntityDataSource>();
            ehDataSource.Setup(s => s.ListAsync(It.IsAny<MonitoringResourceProvider>())).Returns(Task.FromResult(testData));

            var ehManager = new EventHubManagerSimple(ehDataSource.Object, MonitoringResourceProvider.Datadog, logger);

            var item = ehManager.Get("westus");

            // validation
            Assert.NotNull(item);

            item = ehManager.Get("westus");

            // validation
            Assert.NotNull(item);

            item = ehManager.Get("westus");

            // validation
            Assert.NotNull(item);
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public void EventHubManagerInvalidRegionGet()
        {
            var logger = Log.Logger;

            var testData = GenerateTestData(MonitoringResourceProvider.Datadog, "westus", 5);
            var ehDataSource = new Mock<IEventHubEntityDataSource>();
            ehDataSource.Setup(s => s.ListAsync(It.IsAny<MonitoringResourceProvider>())).Returns(Task.FromResult(testData));

            var ehManager = new EventHubManagerSimple(ehDataSource.Object, MonitoringResourceProvider.Datadog, logger);

            var item = ehManager.Get("westus2");

            // validation
            Assert.Null(item);
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public void EventHubManagerMoreThanAvailableGet()
        {
            var logger = Log.Logger;

            var testData = GenerateTestData(MonitoringResourceProvider.Datadog, "westus", 0);
            var ehDataSource = new Mock<IEventHubEntityDataSource>();
            ehDataSource.Setup(s => s.ListAsync(It.IsAny<MonitoringResourceProvider>())).Returns(Task.FromResult(testData));

            var ehManager = new EventHubManagerSimple(ehDataSource.Object, MonitoringResourceProvider.Datadog, logger);

            var item = ehManager.Get("westus");

            // validation
            Assert.Null(item);
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public void EventHubManagerGetSome()
        {
            var logger = Log.Logger;

            var testData = GenerateTestData(MonitoringResourceProvider.Datadog, "westus", 5);
            var ehDataSource = new Mock<IEventHubEntityDataSource>();
            ehDataSource.Setup(s => s.ListAsync(It.IsAny<MonitoringResourceProvider>())).Returns(Task.FromResult(testData));

            var ehManager = new EventHubManagerSimple(ehDataSource.Object, MonitoringResourceProvider.Datadog, logger);

            var item = ehManager.Get("westus", 3);

            // validation
            Assert.NotNull(item);
            Assert.Equal(3, item.Count);
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public void EventHubManagerInvalidRegionGetSome()
        {
            var logger = Log.Logger;

            var testData = GenerateTestData(MonitoringResourceProvider.Datadog, "westus", 5);
            var ehDataSource = new Mock<IEventHubEntityDataSource>();
            ehDataSource.Setup(s => s.ListAsync(It.IsAny<MonitoringResourceProvider>())).Returns(Task.FromResult(testData));

            var ehManager = new EventHubManagerSimple(ehDataSource.Object, MonitoringResourceProvider.Datadog, logger);

            var item = ehManager.Get("westus2", 3);

            // validation
            Assert.Null(item);
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public void EventHubManagerMoreThanAvailableGetSome()
        {
            var logger = Log.Logger;

            var testData = GenerateTestData(MonitoringResourceProvider.Datadog, "westus", 5);
            var ehDataSource = new Mock<IEventHubEntityDataSource>();
            ehDataSource.Setup(s => s.ListAsync(It.IsAny<MonitoringResourceProvider>())).Returns(Task.FromResult(testData));

            var ehManager = new EventHubManagerSimple(ehDataSource.Object, MonitoringResourceProvider.Datadog, logger);

            var item = ehManager.Get("westus", 8);

            // validation
            Assert.NotNull(item);
            Assert.Equal(5, item.Count);
        }

        private IEnumerable<IEventHubEntity> GenerateTestData(MonitoringResourceProvider provider, string location, int count)
        {
            List<IEventHubEntity> testData = new List<IEventHubEntity>();
            var dt = DateTime.UtcNow;
            for (int i = 0; i < count; i++)
            {
                var item = new EventHubEntity()
                {
                    DocumentObjectId = $"id_{i}",
                    ResourceProvider = provider,
                    Namespace = $"namespace_{i}",
                    Name = "test",
                    Location = location,
                    EventHubConnectionString = "test",
                    StorageConnectionString = "test",
                    AuthorizationRuleId = "test",
                    CreatedAtUTC = dt,
                    Active = true,
                };
                testData.Add(item);
            }

            return testData;
        }
    }
}
