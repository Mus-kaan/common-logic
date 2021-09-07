//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.Monitoring.VNext.DiagnosticSettings;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings;
using Moq;
using Serilog;
using Xunit;

namespace Liftr.Monitoring.VNext.Tests
{
    public class DiagnosticSettingsHelperTest
    {
        private DiagnosticSettingsHelper _helper;

        public DiagnosticSettingsHelperTest()
        {
            var loggerMock = new Mock<ILogger>();
            var nameProvider = new DiagnosticSettingsNameProvider(MonitoringResourceProvider.Datadog);
            _helper = new DiagnosticSettingsHelper(nameProvider, loggerMock.Object);
        }

        [Fact]
        public void ExtractMonitoredResourceId()
        {
            var monitordResourceId = _helper.ExtractMonitoredResourceId(VNextTestConstants.ResourceDiagnosticSettingsId);
            Assert.Equal(VNextTestConstants.AcrId, monitordResourceId);
        }

        [Fact]
        public void ExtractDiagnosticSettingsName()
        {
            var dsName = _helper.ExtractDiagnosticSettingsName(VNextTestConstants.ResourceDiagnosticSettingsId);
            Assert.Equal("VNextDS_01", dsName);
        }

        [Fact]
        public void ExtractFullyQualifiedResourceProviderType()
        {
            var resourceProvider = _helper.ExtractFullyQualifiedResourceProviderType(VNextTestConstants.AcrId);
            Assert.Equal("Microsoft.ContainerRegistry/registries", resourceProvider);
        }

        [Fact]
        public void BuildDiagnosticSettingsID()
        {
            var dsName = _helper.BuildDiagnosticSettingsID(VNextTestConstants.AcrId, "DS01");
            Assert.Equal("/subscriptions/db854c4a-c5d8-4dad-955c-0d30d1869217/resourcegroups/unittestsrg/providers/microsoft.containerregistry/registries/vnextacr01/providers/Microsoft.Datadog/ds01", dsName);
        }
    }
}