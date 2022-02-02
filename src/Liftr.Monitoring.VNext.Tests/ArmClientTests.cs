//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Liftr;
using Microsoft.Liftr.Datadog.Whale.Poseidon;
using Microsoft.Liftr.Monitoring.VNext.Whale.Client;
using Microsoft.Liftr.Monitoring.Whale.Options;
using Microsoft.Liftr.TokenManager;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Liftr.Monitoring.VNext.Tests
{
    public class ArmClientTests : IClassFixture<EnvironmentVariablesFixture>
    {
        private readonly ArmClient _armClient;
        private readonly ITestOutputHelper _testOutputHelper;

        public ArmClientTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _armClient = CreateArmClient(_testOutputHelper);
        }

        [Fact(Skip = "For local testing")]
        public async Task GetResourceTestAsync()
        {
            var getResponse = await _armClient.GetResourceAsync(VNextTestConstants.ResourceDiagnosticSettingsId, apiVersion: VNextTestConstants.DiagnosticSettingsV2ApiVersion, VNextTestConstants.TenantId);
            var expectedString = $"\"id\":\"{VNextTestConstants.ResourceDiagnosticSettingsId}\"";
            Assert.Contains(expectedString, getResponse, StringComparison.OrdinalIgnoreCase);
        }

        [Fact(Skip = "For local testing")]
        public async Task GetMultipleResourcesConcurrentlyTestAsync()
        {
            var concurrentRequestsNum = 100;
            var tasks = new List<Task<string>>();
            for (int i = 0; i < concurrentRequestsNum; i++)
            {
                var getTask = _armClient.GetResourceAsync(VNextTestConstants.ResourceDiagnosticSettingsId, apiVersion: VNextTestConstants.DiagnosticSettingsV2ApiVersion, VNextTestConstants.TenantId);
                tasks.Add(getTask);
            }

            string[] getResponses = await Task.WhenAll<string>(tasks);
            foreach (string getResponse in getResponses)
            {
                var expectedString = $"\"id\":\"{VNextTestConstants.ResourceDiagnosticSettingsId}\"";
                Assert.Contains(expectedString, getResponse, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static ArmClient CreateArmClient(ITestOutputHelper testOutputHelper)
        {
            var tokenManagerConfiguration = ConfigurationLoader.GetTokenManagerConfiguration();
#pragma warning disable CA2000 // Dispose objects before losing scope
            var tokenManager = new TokenManager(tokenManagerConfiguration);
#pragma warning restore CA2000 // Dispose objects before losing scope

            using var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var mockTokenManager = new Mock<ITokenManager>();
            mockTokenManager
            .Setup(manager => manager.GetTokenAsync(TestCredentials.ClientId, It.IsAny<string>(), VNextTestConstants.TenantId, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                // testOutputHelper.WriteLine("Getting new Token");
                return tokenManager.GetTokenAsync(TestCredentials.ClientId, TestCredentials.ClientSecret, VNextTestConstants.TenantId);
            });

            var mockAzureClientsProviderOptions = new Mock<AzureClientsProviderOptions>().Object;
            var kvClient = new Mock<IKeyVaultClient>().Object;
            var mockLogger = new Mock<ILogger>().Object;
#pragma warning disable CA2000 // Dispose objects before losing scope
            return new ArmClient(mockAzureClientsProviderOptions, new CertificateStore(kvClient, mockLogger), new HttpClient(), mockLogger);
#pragma warning restore CA2000 // Dispose objects before losing scope
        }
    }
}