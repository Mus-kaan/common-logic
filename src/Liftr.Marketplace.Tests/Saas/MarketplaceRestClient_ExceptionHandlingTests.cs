//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Liftr.Marketplace.Contracts;
using Microsoft.Liftr.Marketplace.Exceptions;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Marketplace.Saas.Tests
{
    public partial class MarketplaceRestClientTests
    {
        [Theory]
        [InlineData("Purchase has failed because we could not find Azure subscription with id d3c0b378-d50b-4ac7-ac42-b9aacc66f6c5​ provided for billing.", PurchaseErrorType.SubscriptionNotFoundForBilling)]
        [InlineData("The plan \"Datadog\" can not be purchased on a free subscription, please upgrade your account, see https://aka.ms/UpgradeFreeSub for more details.", PurchaseErrorType.FreeSubscriptionNotAllowed)]
        [InlineData("The plan \"Confluent Cloud - Pay as you Go\" can not be purchased on a free subscription, please upgrade your account, see https://aka.ms/UpgradeFreeSub for more details.", PurchaseErrorType.FreeSubscriptionNotAllowed)]
        [InlineData("Subscription used for this purchase doesn't allow Marketplace purchases. Use different subscription or ask your administrator to change definition for this subscription and retry.", PurchaseErrorType.MarketplaceNotAllowedForSubscriptions)]
        [InlineData("This offer is not available for purchasing by subscriptions belonging to Microsoft Azure Cloud Solution Providers.", PurchaseErrorType.NotAllowedForCSPSubscriptions)]
        [InlineData("Purchase of offer \"Datadog\" by publisher \"Datadog\" failed with the following error details: Purchase has failed because we couldn't find a valid credit card nor a payment method associated with your Azure subscription. Please use a different Azure subscription or add\\update current credit card or payment method for this subscription and retry.", PurchaseErrorType.MissingPaymentInstrument)]
        public void Throws_purchase_exception_on_Purchase_failure(string errorMessage, PurchaseErrorType errortype)
        {
            var failedPurchaseResponse = new BaseOperationResponse()
            {
                Status = OperationStatus.Failed,
                ErrorStatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = errorMessage,
            };

            using var acceptedResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.Accepted,
            };

            acceptedResponse.Headers.Add(MarketplaceConstants.AsyncOperationLocation, "https://mockmarketplacelocation.com");
            acceptedResponse.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(0.5));

            using var pollingResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(failedPurchaseResponse.ToJson(), Encoding.UTF8, "application/json"),
                RequestMessage = new HttpRequestMessage() { },
            };
            pollingResponse.RequestMessage.Headers.Add("Authorization", "test-bearer-token");

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(acceptedResponse)
                .ReturnsAsync(pollingResponse);

            using var httpClient = new HttpClient(mockMessageHandler.Object, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _marketplaceRestClient = new MarketplaceRestClient(_endpoint, ApiVersion, _logger, _httpClientFactory.Object, () => Task.FromResult("mockToken"));
            Func<Task> act = async () => await _marketplaceRestClient.SendRequestWithPollingAsync<TestResource>(HttpMethod.Put, "/retry/test", content: "somecontent");
            act.Should().Throw<PurchaseFailureException>().Where(e => e.ErrorType == errortype && e.RawErrorMessage.OrdinalEquals(errorMessage));
            Assert.Null(pollingResponse.RequestMessage.Headers.Authorization);
        }
    }
}
