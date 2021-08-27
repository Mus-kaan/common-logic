//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Liftr.Marketplace.Agreement.Interfaces;
using Microsoft.Liftr.Marketplace.Agreement.Models;
using Microsoft.Liftr.Marketplace.Agreement.Service;
using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Models;
using Microsoft.Liftr.TokenManager;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Liftr.Marketplace.Tests.Agreement
{
    public class SignAgreementServiceTests : IDisposable
    {
        private readonly SignAgreementService _agreementService;
        private readonly Mock<IKeyVaultClient> _kvClient;
        private readonly Mock<ISignAgreementRestClient> _signAgreementRestClient;
        private readonly Mock<ILogger> _logger;
        private readonly CertificateStore _certStore;

        private readonly MarketplaceRequestMetadata _marketplaceRequestMetadata = new MarketplaceRequestMetadata()
        {
            MSClientTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47",
            MSClientObjectId = "c6ec8275-0d83-4f4e-88b9-be97b046785a",
            MSClientPrincipalId = "10030000A5D03A4B",
            MSClientIssuer = "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/",
            MSClientPrincipalName = "rohanand@microsoft.com",
        };

        private AgreementResponse _agreementPayload = CreateAgreementPayload();

        public SignAgreementServiceTests()
        {
            using var httpclient = new HttpClient();
            _kvClient = new Mock<IKeyVaultClient>();
            _logger = new Mock<ILogger>();
            _certStore = new CertificateStore(_kvClient.Object, _logger.Object);
            _signAgreementRestClient = new Mock<ISignAgreementRestClient>();
            _agreementService = new SignAgreementService(_signAgreementRestClient.Object);
        }

        [Fact]
        public void AgreementService_InvalidParameters_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new SignAgreementService(null));
            Assert.Throws<ArgumentNullException>(() => new SignAgreementService(_signAgreementRestClient.Object, null));
        }

        [Fact]
        public async Task GetAgreementAsync_Invalid_Parameters_Throws_Async()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _agreementService.GetAgreementAsync(new MarketplaceSaasResourceProperties(), _marketplaceRequestMetadata));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _agreementService.GetAgreementAsync(CreateSaasResourceProperties("test"), new MarketplaceRequestMetadata()));
        }

        [Fact]
        public async Task GetandSignAgreementAsync_Invalid_Parameters_Throws_Async()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _agreementService.GetandSignAgreementAsync(new MarketplaceSaasResourceProperties(), _marketplaceRequestMetadata));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _agreementService.GetandSignAgreementAsync(CreateSaasResourceProperties("test"), new MarketplaceRequestMetadata()));
        }

        [Fact]
        public async Task SignAgreementAsync_Invalid_Parameters_Throws_Async()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _agreementService.SignAgreementAsync(new MarketplaceSaasResourceProperties(), _marketplaceRequestMetadata, _agreementPayload));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _agreementService.SignAgreementAsync(CreateSaasResourceProperties("test"), new MarketplaceRequestMetadata(), _agreementPayload));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _agreementService.SignAgreementAsync(CreateSaasResourceProperties("test"), _marketplaceRequestMetadata, new AgreementResponse()));
        }

        [Fact]
        public async Task GetAgreement_Expected_Behavior_Async()
        {
            var marketplaceResourceProperties = CreateSaasResourceProperties("test");
            var agreementResponse = CreateAgreementPayload();

            _signAgreementRestClient
                .Setup(x => x.SendRequestAsync<AgreementResponse>(HttpMethod.Get, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agreementResponse);

            var response = await _agreementService.GetAgreementAsync(marketplaceResourceProperties, _marketplaceRequestMetadata);
            Assert.Equal(agreementResponse.Id, response.Id);
            Assert.Equal(agreementResponse.Properties.LicenseTextLink, response.Properties.LicenseTextLink);
        }

        [Fact]
        public async Task SignAgreement_Expected_Behavior_Async()
        {
            var marketplaceResourceProperties = CreateSaasResourceProperties("test");
            var agreementResponse = CreateAgreementPayload();
            agreementResponse.Properties.Accepted = true;

            _signAgreementRestClient
                .Setup(x => x.SendRequestAsync<AgreementResponse>(HttpMethod.Put, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agreementResponse);

            var response = await _agreementService.SignAgreementAsync(marketplaceResourceProperties, _marketplaceRequestMetadata, agreementResponse);
            Assert.Equal(agreementResponse.Id, response.Id);
            Assert.True(response.Properties.Accepted);
            Assert.Equal(agreementResponse.Properties.LicenseTextLink, response.Properties.LicenseTextLink);
        }

        [Fact]
        public async Task GetAndSignAgreement_Expected_Behavior_Async()
        {
            var marketplaceResourceProperties = CreateSaasResourceProperties("test");
            var agreementResponse = CreateAgreementPayload();
            agreementResponse.Properties.Accepted = true;

            _signAgreementRestClient
                .Setup(x => x.SendRequestAsync<AgreementResponse>(It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agreementResponse);

            var response = await _agreementService.SignAgreementAsync(marketplaceResourceProperties, _marketplaceRequestMetadata, agreementResponse);
            Assert.Equal(agreementResponse.Id, response.Id);
            Assert.True(response.Properties.Accepted);
            Assert.Equal(agreementResponse.Properties.LicenseTextLink, response.Properties.LicenseTextLink);
        }

        [Fact]
        public async Task GetAgreement_Expected_Behavior_UsingTokenService_Async()
        {
            var marketplaceResourceProperties = CreateSaasResourceProperties("test");
            var agreementResponse = CreateAgreementPayload();

            _signAgreementRestClient
                .Setup(x => x.SendRequestUsingTokenServiceAsync<AgreementResponse>(HttpMethod.Get, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agreementResponse);

            var response = await _agreementService.GetAgreementUsingTokenServiceAsync(marketplaceResourceProperties, _marketplaceRequestMetadata);
            Assert.Equal(agreementResponse.Id, response.Id);
            Assert.Equal(agreementResponse.Properties.LicenseTextLink, response.Properties.LicenseTextLink);
        }

        [Fact]
        public async Task SignAgreement_Expected_Behavior_UsingTokenService_Async()
        {
            var marketplaceResourceProperties = CreateSaasResourceProperties("test");
            var agreementResponse = CreateAgreementPayload();
            agreementResponse.Properties.Accepted = true;

            _signAgreementRestClient
                .Setup(x => x.SendRequestUsingTokenServiceAsync<AgreementResponse>(HttpMethod.Put, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agreementResponse);

            var response = await _agreementService.SignAgreementUsingTokenServiceAsync(marketplaceResourceProperties, _marketplaceRequestMetadata, agreementResponse);
            Assert.Equal(agreementResponse.Id, response.Id);
            Assert.True(response.Properties.Accepted);
            Assert.Equal(agreementResponse.Properties.LicenseTextLink, response.Properties.LicenseTextLink);
        }

        [Fact]
        public async Task GetAndSignAgreement_Expected_Behavior_UsingTokenService_Async()
        {
            var marketplaceResourceProperties = CreateSaasResourceProperties("test");
            var agreementResponse = CreateAgreementPayload();
            agreementResponse.Properties.Accepted = true;

            _signAgreementRestClient
                .Setup(x => x.SendRequestUsingTokenServiceAsync<AgreementResponse>(It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agreementResponse);

            var response = await _agreementService.SignAgreementUsingTokenServiceAsync(marketplaceResourceProperties, _marketplaceRequestMetadata, agreementResponse);
            Assert.Equal(agreementResponse.Id, response.Id);
            Assert.True(response.Properties.Accepted);
            Assert.Equal(agreementResponse.Properties.LicenseTextLink, response.Properties.LicenseTextLink);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _certStore?.Dispose();
            }
        }

        private static MarketplaceSaasResourceProperties CreateSaasResourceProperties(string resourceName)
        {
            return new MarketplaceSaasResourceProperties()
            {
                PublisherId = "isvtestuklegacy",
                OfferId = "liftr_saas_offer",
                Name = resourceName,
                PlanId = "liftsaasplan1",
                PaymentChannelType = "SubscriptionDelegated",
                TermId = "hjdtn7tfnxcy",
                PaymentChannelMetadata = new PaymentChannelMetadata()
                {
                    AzureSubscriptionId = "f5f53739-49e7-49a4-b5b1-00c63a7961a1",
                },
            };
        }

        private static AgreementResponse CreateAgreementPayload()
        {
            return new AgreementResponse()
            {
                Id = "id",
                Name = "resource",
                Type = "type",
                Properties = new AgreementResponseProperties()
                {
                    Accepted = false,
                    LicenseTextLink = "licenseText",
                    MarketplaceTermsLink = "https://mpTerms.com",
                    Plan = "PlanId",
                    Publisher = "rohit",
                    Product = "product",
                },
            };
        }
    }
}
