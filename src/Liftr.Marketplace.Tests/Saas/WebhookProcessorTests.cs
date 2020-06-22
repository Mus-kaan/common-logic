//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using Microsoft.Liftr.Marketplace.Saas.Interfaces;
using Moq;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Marketplace.Saas.Tests
{
    public class WebhookProcessorTests
    {
        [Fact]
        public async Task Calls_get_operation_api_and_doesnt_call_webhook_handler_if_operation_api_throws_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.Unsubscribe);

            webhookProcessor.FulfillmentClientMock.Setup(client => client.GetOperationAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>())).ThrowsAsync(new MarketplaceException());

            await Assert.ThrowsAsync<MarketplaceException>(async () => await webhookProcessor.ProcessWebhookAsync(payload));

            webhookProcessor.VerifyGetOperationsIsCalled(payload);
            webhookProcessor.VerifyNoOtherFulfillmentCalls();
            webhookProcessor.VerifyNoOtherWebhookHandlerCalls();
        }

        [Fact]
        public async Task If_handler_returns_success_on_delete_then_updates_operation_status_with_success_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.Unsubscribe);

            webhookProcessor.FulfillmentClientMock
                .Setup(client => client.GetOperationAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessDeleteAsync(payload.MarketplaceSubscription)).ReturnsAsync(OperationUpdateStatus.Success);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerDeleteIsCalled();
            webhookProcessor.VerifyUpdateOperationsCalled(payload, OperationUpdateStatus.Success);
        }

        [Fact]
        public async Task If_handler_returns_failure_on_delete_then_updates_operation_status_with_failure_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.Unsubscribe);

            webhookProcessor.FulfillmentClientMock
                .Setup(client => client.GetOperationAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessDeleteAsync(payload.MarketplaceSubscription)).ReturnsAsync(OperationUpdateStatus.Failure);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerDeleteIsCalled();
            webhookProcessor.VerifyUpdateOperationsCalled(payload, OperationUpdateStatus.Failure);
        }

        private WebhookPayload CreateWebhook(WebhookAction action)
        {
            var operationId = Guid.NewGuid();
            var subscription = new MarketplaceSubscription(Guid.NewGuid());

            return new WebhookPayload()
            {
                OperationId = operationId,
                MarketplaceSubscription = subscription,
                Action = action,
                OfferId = "TestOfferId",
                PlanId = "TestPlanId",
                PublisherId = "TestPublisherId",
            };
        }
    }

    internal class TestWebhookProcessor
    {
        public TestWebhookProcessor()
        {
            FulfillmentClientMock = new Mock<IMarketplaceFulfillmentClient>();
            WebhookHandlerMock = new Mock<IWebhookHandler>();
            Logger = new Mock<ILogger>().Object;
        }

        public Mock<IMarketplaceFulfillmentClient> FulfillmentClientMock { get; }

        public Mock<IWebhookHandler> WebhookHandlerMock { get; }

        public ILogger Logger { get; }

        public async Task ProcessWebhookAsync(WebhookPayload payload)
        {
            var webhookProcessor = new WebhookProcessor(FulfillmentClientMock.Object, WebhookHandlerMock.Object, Logger);
            await webhookProcessor.ProcessWebhookNotificationAsync(payload);
        }

        public void VerifyWebhookHandlerDeleteIsCalled()
        {
            WebhookHandlerMock.Verify(handler => handler.ProcessDeleteAsync(It.IsAny<MarketplaceSubscription>()), Times.Once());
            VerifyNoOtherWebhookHandlerCalls();
        }

        public void VerifyGetOperationsIsCalled(WebhookPayload payload)
        {
            FulfillmentClientMock.Verify(client => client.GetOperationAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()), Times.Once());
        }

        public void VerifyUpdateOperationsCalled(WebhookPayload payload, OperationUpdateStatus operationUpdateStatus)
        {
            FulfillmentClientMock.Verify(
                client => client.UpdateOperationAsync(
                    payload.MarketplaceSubscription,
                    payload.OperationId,
                    It.Is<OperationUpdate>(update => update.Status == operationUpdateStatus),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        public void VerifyNoOtherFulfillmentCalls()
        {
            FulfillmentClientMock.VerifyNoOtherCalls();
        }

        public void VerifyNoOtherWebhookHandlerCalls()
        {
            WebhookHandlerMock.VerifyNoOtherCalls();
        }
    }
}
