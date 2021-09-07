//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.VNext.Whale;
using Microsoft.Liftr.Monitoring.VNext.Whale.Interfaces;
using Microsoft.Liftr.Monitoring.VNext.Whale.Models;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Models;
using Microsoft.Liftr.RPaaS;
using MongoDB.Bson;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Monitoring.VNext.Tests.Whale
{
    public class WhaleMessageProcessorTests
    {
        private readonly Mock<IMetaRPWhaleService> _metaRPWhaleServiceMock;
        private readonly Mock<IWhaleFilterClient> _whaleFilterClientMock;
        private readonly Mock<IMonitoredResourceManager> _resourceManagerMock;
        private readonly Mock<IPartnerResourceDataSource<PartnerResourceEntity>> _partnerDataSourceMock;
        private readonly Mock<IMetricsRulesUpdateService> _metricsRulesUpdateServiceMock;
        private readonly IMetaRPWhaleService _metaRPWhaleService;
        private readonly IWhaleFilterClient _whaleFilterClient;
        private readonly IMonitoredResourceManager _resourceManager;
        private readonly IPartnerResourceDataSource<PartnerResourceEntity> _partnerDataSource;
        private readonly IMetricsRulesUpdateService _metricsRulesUpdateService;
        private readonly ILogger _logger;

        public WhaleMessageProcessorTests()
        {
            _metaRPWhaleServiceMock = new Mock<IMetaRPWhaleService>();
            _whaleFilterClientMock = new Mock<IWhaleFilterClient>();
            _resourceManagerMock = new Mock<IMonitoredResourceManager>();
            _partnerDataSourceMock = new Mock<IPartnerResourceDataSource<PartnerResourceEntity>>();
            _metricsRulesUpdateServiceMock = new Mock<IMetricsRulesUpdateService>();
            var loggerMock = new Mock<ILogger>();

            var tagRules = new MonitoringTagRules();

            _partnerDataSourceMock
                .Setup(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PartnerResourceEntity() { ResourceId = "partnerId" });

            _metaRPWhaleService = _metaRPWhaleServiceMock.Object;
            _whaleFilterClient = _whaleFilterClientMock.Object;
            _resourceManager = _resourceManagerMock.Object;
            _partnerDataSource = _partnerDataSourceMock.Object;
            _metricsRulesUpdateService = _metricsRulesUpdateServiceMock.Object;
            _logger = loggerMock.Object;
        }

        [Fact]
        public void WhaleMessageProcessor_InvalidParameters_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new WhaleMessageProcessor(
                null, _whaleFilterClient, _metricsRulesUpdateService, _resourceManager, _partnerDataSource, _logger));

            Assert.Throws<ArgumentNullException>(() => new WhaleMessageProcessor(
                _metaRPWhaleService, null, _metricsRulesUpdateService, _resourceManager, _partnerDataSource, _logger));

            Assert.Throws<ArgumentNullException>(() => new WhaleMessageProcessor(
                _metaRPWhaleService, _whaleFilterClient, null, _resourceManager, _partnerDataSource, _logger));

            Assert.Throws<ArgumentNullException>(() => new WhaleMessageProcessor(
                _metaRPWhaleService, _whaleFilterClient, _metricsRulesUpdateService, null, _partnerDataSource, _logger));

            Assert.Throws<ArgumentNullException>(() => new WhaleMessageProcessor(
                _metaRPWhaleService, _whaleFilterClient, _metricsRulesUpdateService, _resourceManager, null, _logger));

            Assert.Throws<ArgumentNullException>(() => new WhaleMessageProcessor(
                _metaRPWhaleService, _whaleFilterClient, _metricsRulesUpdateService, _resourceManager, _partnerDataSource, null));
        }

        [Fact]
        public async Task ProcessUpdateTagRulesMessageAsync_InvalidParameters_ThrowsExceptionAsync()
        {
            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            await Assert.ThrowsAsync<ArgumentNullException>(() => messageProcessor.ProcessUpdateTagRulesMessageAsync(null, "tenantId"));

            await Assert.ThrowsAsync<ArgumentNullException>(() => messageProcessor.ProcessUpdateTagRulesMessageAsync(
                "/subscriptions/mysub/resourceGroups/myRg/providers/Microsoft.Datadog/monitors/myMonitor", null));
        }

        [Fact]
        public async Task ProcessUpdateTagRulesMessageAsync_MetricsAndLogsFiltersDisabled_ExpectedBehaviorAsync()
        {
            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitoringTagRulesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new MonitoringTagRules()
                {
                    Properties = new MonitoringTagRulesProperties()
                    {
                        MetricRules = new MetricRules()
                        {
                            FilteringTags = new List<FilteringTag>()
                            {
                                new FilteringTag()
                                {
                                    Action = TagAction.Include,
                                    Name = "naruto",
                                    Value = "shippuden",
                                },
                            },
                        },
                        LogRules = new LogRules()
                        {
                            SendSubscriptionLogs = false,
                            SendActivityLogs = false,
                        },
                    },
                });

            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitorResourceDetailsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new MonitorResourceDetails()
                {
                    Location = "West Us 2",
                    MonitoringStatus = Microsoft.Liftr.Monitoring.VNext.Whale.Models.MonitoringStatus.Disabled,
                    MonitoringPartnerEntityId = ObjectId.GenerateNewId().ToString(),
                    ProvisioningState = ProvisioningState.Succeeded,
                });

            _metricsRulesUpdateServiceMock
                .Setup(r => r.UpdateMetricRulesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string monitorId, string tenantId) =>
                {
                    Assert.Equal("/subscriptions/mysub/resourceGroups/myRg/providers/Microsoft.Datadog/monitors/myMonitor", monitorId);
                    Assert.Equal("tenantId", tenantId);
                });

            _resourceManagerMock
                .Setup(r => r.StopMonitoringSubscriptionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string monitorId, string partnerEntityId, string tenantId) =>
                {
                    Assert.Equal("/subscriptions/mysub/resourceGroups/myRg/providers/Microsoft.Datadog/monitors/myMonitor", monitorId);
                    Assert.Equal("tenantId", tenantId);
                });

            _resourceManagerMock
                .Setup(r => r.ListMonitoredResourcesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<MonitoredResource>()
                {
                    new MonitoredResource() { Id = "existing1" },
                });

            _resourceManagerMock
                .Setup(r => r.ListTrackedResourcesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringStatus>()
                {
                    new Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringStatus()
                    {
                        MonitoredResourceId = "existing2",
                        IsMonitored = false,
                    },
                });

            _resourceManagerMock
                .Setup(r => r.StopMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((MonitoredResource resource, string monitorId, string partnerEntityId, string tenantId) =>
                {
                    Assert.Equal("existing1", resource.Id);
                });

            _resourceManagerMock
                .Setup(r => r.StopTrackingResourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string resourceId, string monitorId, string partnerEntityId, string tenantId) =>
                {
                    Assert.Equal("existing2", resourceId);
                });

            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            await messageProcessor.ProcessUpdateTagRulesMessageAsync(
                "/subscriptions/mysub/resourceGroups/myRg/providers/Microsoft.Datadog/monitors/myMonitor", "tenantId");

            // Metrics Rules Update call
            _metricsRulesUpdateServiceMock.Verify(
                r => r.UpdateMetricRulesAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            // Whale filter client calls
            _whaleFilterClientMock.VerifyNoOtherCalls();
            _whaleFilterClientMock.Invocations.Clear();

            // Monitored resource manager calls
            _resourceManagerMock.Verify(
                r => r.StopMonitoringSubscriptionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            _resourceManagerMock.Verify(r => r.ListMonitoredResourcesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
            _resourceManagerMock.Verify(
                r => r.StopMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            _resourceManagerMock.Verify(r => r.ListTrackedResourcesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
            _resourceManagerMock.Verify(
                r => r.StopTrackingResourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            _resourceManagerMock.VerifyNoOtherCalls();
            _resourceManagerMock.Invocations.Clear();
        }

        [Fact]
        public async Task ProcessUpdateTagRulesMessageAsync_MetricsAndLogsFiltersEnabled_ExpectedBehaviorAsync()
        {
            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitoringTagRulesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new MonitoringTagRules()
                {
                    Properties = new MonitoringTagRulesProperties()
                    {
                        MetricRules = new MetricRules()
                        {
                            FilteringTags = new List<FilteringTag>()
                            {
                                new FilteringTag()
                                {
                                    Name = "A",
                                    Value = "B",
                                    Action = TagAction.Include,
                                },
                                new FilteringTag()
                                {
                                    Name = "C",
                                    Value = "D",
                                    Action = TagAction.Exclude,
                                },
                            },
                        },
                        LogRules = new LogRules()
                        {
                            SendSubscriptionLogs = true,
                            SendActivityLogs = true,
                        },
                    },
                });

            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitorResourceDetailsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new MonitorResourceDetails()
                {
                    Location = "West Us 2",
                    MonitoringStatus = VNext.Whale.Models.MonitoringStatus.Enabled,
                    MonitoringPartnerEntityId = ObjectId.GenerateNewId().ToString(),
                    ProvisioningState = ProvisioningState.Succeeded,
                });

            _whaleFilterClientMock
                .Setup(w => w.ListResourcesByTagsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<FilteringTag>>()))
                .ReturnsAsync(new List<MonitoredResource>()
                {
                    new MonitoredResource() { Id = "new1" },
                    new MonitoredResource() { Id = "Existing1" },
                });

            _metricsRulesUpdateServiceMock
                .Setup(r => r.UpdateMetricRulesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string monitorId, string tenantId) =>
                {
                    Assert.Equal("/subscriptions/mysub/resourceGroups/myRg/providers/Microsoft.Datadog/monitors/myMonitor", monitorId);
                    Assert.Equal("tenantId", tenantId);
                });

            _resourceManagerMock
                .Setup(r => r.StartMonitoringSubscriptionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string monitorId, string location, string partnerEntityId, string tenantId) =>
                {
                    Assert.Equal("/subscriptions/mysub/resourceGroups/myRg/providers/Microsoft.Datadog/monitors/myMonitor", monitorId);
                    Assert.Equal("tenantId", tenantId);
                });

            _resourceManagerMock
                .Setup(r => r.ListMonitoredResourcesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<MonitoredResource>()
                {
                    new MonitoredResource() { Id = "existing1" },
                    new MonitoredResource() { Id = "existing2" },
                });

            _resourceManagerMock
                .Setup(r => r.ListTrackedResourcesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringStatus>() { });

            _resourceManagerMock
                .Setup(r => r.StartMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((MonitoredResource resource, string monitorId, string partnerEntityId, string tenantId) =>
                {
                    Assert.Equal("new1", resource.Id);
                });

            _resourceManagerMock
                .Setup(r => r.StopMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((MonitoredResource resource, string monitorId, string partnerEntityId, string tenantId) =>
                {
                    Assert.Equal("existing2", resource.Id);
                });

            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            await messageProcessor.ProcessUpdateTagRulesMessageAsync(
                "/subscriptions/mysub/resourceGroups/myRg/providers/Microsoft.Datadog/monitors/myMonitor", "tenantId");

            // Metrics Rules Update call
            _metricsRulesUpdateServiceMock.Verify(
                r => r.UpdateMetricRulesAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            // Whale filter client calls
            _whaleFilterClientMock.Verify(
                w => w.ListResourcesByTagsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<FilteringTag>>()),
                Times.Exactly(1));

            _whaleFilterClientMock.VerifyNoOtherCalls();
            _whaleFilterClientMock.Invocations.Clear();

            // Monitored resource manager calls
            _resourceManagerMock.Verify(
                r => r.StartMonitoringSubscriptionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            _resourceManagerMock.Verify(r => r.ListMonitoredResourcesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
            _resourceManagerMock.Verify(r => r.ListTrackedResourcesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
            _resourceManagerMock.Verify(
                r => r.StartMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            _resourceManagerMock.Verify(
                r => r.StopMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            _resourceManagerMock.VerifyNoOtherCalls();
            _resourceManagerMock.Invocations.Clear();
        }

        [Fact]
        public async Task ProcessAutoMonitoringMessageAsync_InvalidParameters_ThrowsExceptionAsync()
        {
            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            await Assert.ThrowsAsync<ArgumentNullException>(() => messageProcessor.ProcessAutoMonitoringMessageAsync(
                null, "tenantId"));

            await Assert.ThrowsAsync<ArgumentNullException>(() => messageProcessor.ProcessAutoMonitoringMessageAsync(
                "objectId", null));
        }

        [Fact]
        public async Task ProcessAutoMonitoringMessageAsync_MetricsAndLogsFiltersEnabled_ExpectedBehaviorAsync()
        {
            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitoringTagRulesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new MonitoringTagRules()
                {
                    Properties = new MonitoringTagRulesProperties()
                    {
                        MetricRules = new MetricRules()
                        {
                            FilteringTags = new List<FilteringTag>()
                            {
                                new FilteringTag()
                                {
                                    Name = "A",
                                    Value = "B",
                                    Action = TagAction.Include,
                                },
                                new FilteringTag()
                                {
                                    Name = "C",
                                    Value = "D",
                                    Action = TagAction.Exclude,
                                },
                            },
                        },
                        LogRules = new LogRules()
                        {
                            SendSubscriptionLogs = true,
                            SendActivityLogs = true,
                        },
                    },
                });

            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitorResourceDetailsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new MonitorResourceDetails()
                {
                    Location = "West Us 2",
                    MonitoringStatus = VNext.Whale.Models.MonitoringStatus.Enabled,
                    MonitoringPartnerEntityId = ObjectId.GenerateNewId().ToString(),
                    ProvisioningState = ProvisioningState.Succeeded,
                });

            _whaleFilterClientMock
                .Setup(w => w.ListResourcesByTagsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<FilteringTag>>()))
                .ReturnsAsync(new List<MonitoredResource>()
                {
                    new MonitoredResource() { Id = "new1" },
                    new MonitoredResource() { Id = "Existing1" },
                });

            _metricsRulesUpdateServiceMock
                .Setup(r => r.UpdateMetricRulesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string monitorId, string tenantId) =>
                {
                    Assert.Equal("/subscriptions/mysub/resourceGroups/myRg/providers/Microsoft.Datadog/monitors/myMonitor", monitorId);
                    Assert.Equal("tenantId", tenantId);
                });

            _resourceManagerMock
                .Setup(r => r.ListMonitoredResourcesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<MonitoredResource>()
                {
                    new MonitoredResource() { Id = "existing1" },
                    new MonitoredResource() { Id = "existing2" },
                });

            _resourceManagerMock
                .Setup(r => r.ListTrackedResourcesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringStatus>() { });

            _resourceManagerMock
                .Setup(r => r.StartMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((MonitoredResource resource, string monitorId, string partnerEntityId, string tenantId) =>
                {
                    Assert.Equal("new1", resource.Id);
                });

            _resourceManagerMock
                .Setup(r => r.StopMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((MonitoredResource resource, string monitorId, string partnerEntityId, string tenantId) =>
                {
                    Assert.Equal("existing2", resource.Id);
                });

            _partnerDataSourceMock
                .Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PartnerResourceEntity()
                {
                    ResourceId = "/subscriptions/mysub/resourceGroups/myRg/providers/Microsoft.Datadog/monitors/myMonitor",
                });

            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            await messageProcessor.ProcessAutoMonitoringMessageAsync(
                "objectId", "tenantId");

            // Metrics Rules Update call
            _metricsRulesUpdateServiceMock.Verify(
                r => r.UpdateMetricRulesAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            // Whale filter client calls
            _whaleFilterClientMock.Verify(
                w => w.ListResourcesByTagsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<FilteringTag>>()),
                Times.Exactly(1));

            _whaleFilterClientMock.VerifyNoOtherCalls();
            _whaleFilterClientMock.Invocations.Clear();

            // Monitored resource manager calls
            _resourceManagerMock.Verify(r => r.ListMonitoredResourcesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
            _resourceManagerMock.Verify(r => r.ListTrackedResourcesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
            _resourceManagerMock.Verify(
                r => r.StartMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            _resourceManagerMock.Verify(
                r => r.StopMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            _resourceManagerMock.VerifyNoOtherCalls();
            _resourceManagerMock.Invocations.Clear();
        }

        [Fact]
        public async Task ProcessAutoMonitoringMessageAsync_DeletedMonitorResource_DoesNothingAsync()
        {
            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitorResourceDetailsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new MetaRPException() { StatusCode = HttpStatusCode.NotFound });

            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            var provisioningState = await messageProcessor.ProcessAutoMonitoringMessageAsync("objectId", "tenantId");

            Assert.Equal(ProvisioningState.Deleted, provisioningState);

            _whaleFilterClientMock.VerifyNoOtherCalls();
            _resourceManagerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessAutoMonitoringMessageAsync_PartnerDataSourceEntityNotFound_DoesNothingAsync()
        {
            PartnerResourceEntity partnerResourceEntity = null;

            _partnerDataSourceMock
                .Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(partnerResourceEntity);

            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            var provisioningState = await messageProcessor.ProcessAutoMonitoringMessageAsync("objectId", "tenantId");

            Assert.Equal(ProvisioningState.Deleted, provisioningState);

            _whaleFilterClientMock.VerifyNoOtherCalls();
            _resourceManagerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessAutoMonitoringMessageAsync_MonitorResourceBeingDeleted_DoesNothingAsync()
        {
            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitorResourceDetailsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new MonitorResourceDetails()
                { ProvisioningState = ProvisioningState.Deleting });

            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            var provisioningState = await messageProcessor.ProcessAutoMonitoringMessageAsync("objectId", "tenantId");

            Assert.Equal(ProvisioningState.Deleting, provisioningState);

            _whaleFilterClientMock.VerifyNoOtherCalls();
            _resourceManagerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessAutoMonitoringMessageAsync_NonExistingTagRulesResource_DoesNothingAsync()
        {
            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitorResourceDetailsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new MonitorResourceDetails()
                { ProvisioningState = ProvisioningState.Succeeded });

            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitoringTagRulesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new MetaRPException() { StatusCode = HttpStatusCode.NotFound });

            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            var provisioningState = await messageProcessor.ProcessAutoMonitoringMessageAsync("objectId", "tenantId");

            Assert.Equal(ProvisioningState.Creating, provisioningState);

            _whaleFilterClientMock.VerifyNoOtherCalls();
            _resourceManagerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessAutoMonitoringMessageAsync_MetaRPException_ThrowsAsync()
        {
            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitorResourceDetailsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new MonitorResourceDetails()
                { ProvisioningState = ProvisioningState.Succeeded });

            _metaRPWhaleServiceMock
                .Setup(m => m.GetMonitoringTagRulesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new MetaRPException() { StatusCode = HttpStatusCode.InternalServerError });

            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            await Assert.ThrowsAsync<MetaRPException>(
                async () => await messageProcessor.ProcessAutoMonitoringMessageAsync("objectId", "tenantId"));
        }

        [Fact]
        public async Task ProcessDeleteMessageAsync_InvalidParameters_ThrowsExceptionAsync()
        {
            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            await Assert.ThrowsAsync<ArgumentNullException>(() => messageProcessor.ProcessDeleteMessageAsync(
                null, "tenantId"));

            await Assert.ThrowsAsync<ArgumentNullException>(() => messageProcessor.ProcessDeleteMessageAsync(
                "objectId", null));
        }

        [Fact]
        public async Task ProcessDeleteMessageAsync_PartnerDataSourceEntityNotFound_DoesNothingAsync()
        {
            PartnerResourceEntity partnerResourceEntity = null;

            _partnerDataSourceMock
                .Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(partnerResourceEntity);

            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleServiceMock.Object,
                _whaleFilterClientMock.Object,
                _metricsRulesUpdateServiceMock.Object,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            await messageProcessor.ProcessDeleteMessageAsync("objectId", "myTenant");

            _whaleFilterClientMock.VerifyNoOtherCalls();
            _resourceManagerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessDeleteMessageAsync_ValidParameters_ExpectedBehaviorAsync()
        {
            _resourceManagerMock
                .Setup(r => r.StopMonitoringSubscriptionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string monitorId, string partnerEntityId, string tenantId) =>
                {
                    Assert.Equal("myTenant", tenantId);
                });

            _resourceManagerMock
                .Setup(r => r.ListMonitoredResourcesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<MonitoredResource>()
                {
                    new MonitoredResource() { Id = "existing1" },
                });

            _resourceManagerMock
                .Setup(r => r.ListTrackedResourcesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringStatus>()
                {
                    new Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringStatus()
                    {
                        MonitoredResourceId = "existing2",
                        IsMonitored = false,
                    },
                });

            _resourceManagerMock
                .Setup(r => r.StopMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((MonitoredResource resource, string monitorId, string partnerEntityId, string tenantId) =>
                {
                    Assert.Equal("existing1", resource.Id);
                });

            _resourceManagerMock
                .Setup(r => r.StopTrackingResourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string resourceId, string monitorId, string partnerEntityId, string tenantId) =>
                {
                    Assert.Equal("existing2", resourceId);
                });

            var messageProcessor = new WhaleMessageProcessor(
                _metaRPWhaleService,
                _whaleFilterClient,
                _metricsRulesUpdateService,
                _resourceManagerMock.Object,
                _partnerDataSourceMock.Object,
                _logger);

            await messageProcessor.ProcessDeleteMessageAsync("objectId", "myTenant");

            _resourceManagerMock.Verify(r => r.ListMonitoredResourcesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
            _resourceManagerMock.Verify(
                r => r.StopMonitoringResourceAsync(It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            _resourceManagerMock.Verify(r => r.ListTrackedResourcesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
            _resourceManagerMock.Verify(
                r => r.StopTrackingResourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(1));

            _resourceManagerMock
                .Verify(
                r => r.StopMonitoringSubscriptionAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once());

            _resourceManagerMock
                .Verify(
                r => r.StopMonitoringResourceAsync(
                    It.IsAny<MonitoredResource>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once());

            _resourceManagerMock.Invocations.Clear();
        }
    }
}
