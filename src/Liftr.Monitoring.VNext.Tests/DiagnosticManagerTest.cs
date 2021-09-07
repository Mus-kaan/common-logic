//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.Monitoring.VNext.DiagnosticSettings;
using Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.Datadog.Whale.Poseidon;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model.Builders;
using Microsoft.Liftr.Monitoring.VNext.Whale.Client.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Moq;
using Serilog;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Liftr.Monitoring.VNext.Tests
{
    public sealed class DiagnosticManagerTest : IClassFixture<EnvironmentVariablesFixture>, IDisposable
    {
        private readonly IAzureClientsProvider _azureClientsProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly DiagnosticSettingsManager _dsManager;
        private readonly ITestOutputHelper _testOutputHelper;

        public DiagnosticManagerTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _azureClientsProvider = new PoseidonClientsProvider();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            var logger = new Mock<ILogger>().Object;
            var nameProvider = new DiagnosticSettingsNameProvider(MonitoringResourceProvider.Datadog);
            var dsHelper = new DiagnosticSettingsHelper(nameProvider, logger);
            var dsResourceModelBuilder = new DiagnosticSettingsResourceModelBuilder(_memoryCache, dsHelper, logger);
            var dsSubModelBuilder = new DiagnosticSettingsSubscriptionModelBuilder();
            var armClient = ArmClientTests.CreateArmClient(_testOutputHelper);
            _dsManager = new DiagnosticSettingsManager(_azureClientsProvider, dsResourceModelBuilder, dsSubModelBuilder, nameProvider, armClient, logger);
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
        }

        [Fact(Skip = "For local testing")]
        public async Task GetDiagnosticSettingsAsync()
        {
            var dsResult = await _dsManager.GetResourceDiagnosticSettingsAsync(VNextTestConstants.ResourceDiagnosticSettingsId, VNextTestConstants.TenantId);

            Assert.True(dsResult.SuccessfulOperation);
            Assert.NotNull(dsResult.DiagnosticSettingsName);
        }

        [Fact(Skip = "For local testing")]
        public async Task CreateAndRemoveResourceDiagnosticSettingsAsync()
        {
            var createResult = await _dsManager.CreateOrUpdateResourceDiagnosticSettingAsync(VNextTestConstants.CreateAcrId, VNextTestConstants.DatadogResourceId, VNextTestConstants.TenantId);

            Assert.True(createResult.SuccessfulOperation);
            Assert.NotNull(createResult.DiagnosticSettingsName);

            var removeResult = await _dsManager.RemoveResourceDiagnosticSettingAsync(VNextTestConstants.CreateAcrId, createResult.DiagnosticSettingsName, VNextTestConstants.TenantId);
            Assert.True(createResult.SuccessfulOperation);
        }

        [Fact(Skip = "For local testing")]
        public async Task CreateSubscriptionDiagnosticSettingsAsync()
        {
            var createResult = await _dsManager.CreateOrUpdateSubscriptionDiagnosticSettingAsync(VNextTestConstants.SubscriptionId, VNextTestConstants.DatadogResourceId, VNextTestConstants.TenantId);

            Assert.True(createResult.SuccessfulOperation);
            Assert.NotNull(createResult.DiagnosticSettingsName);

            var removeResult = await _dsManager.RemoveSubscriptionDiagnosticSettingAsync(VNextTestConstants.SubscriptionId, createResult.DiagnosticSettingsName, VNextTestConstants.DatadogResourceId, VNextTestConstants.TenantId);
            Assert.True(createResult.SuccessfulOperation);
        }
    }
}