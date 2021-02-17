//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

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
        public async Task ProcessWebhook_Throws_Exception_When_Payload_Is_Null_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            WebhookPayload payload = null;

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await webhookProcessor.ProcessWebhookAsync(payload));
        }

        [Fact]
        public async Task ProcessWebhook_Throws_Exception_When_Payload_Marketplace_Subscription_Is_Null_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            WebhookPayload payload = CreateWebhook(WebhookAction.Unsubscribe);
            payload.MarketplaceSubscription = null;

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await webhookProcessor.ProcessWebhookAsync(payload));
        }

        [Fact]
        public async Task ProcessWebhook_Throws_Exception_When_Payload_Action_Is_Undefined_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            WebhookPayload payload = CreateWebhook(WebhookAction.Unsubscribe);
            payload.Action = (WebhookAction)9;

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await webhookProcessor.ProcessWebhookAsync(payload));
        }

        [Fact]
        public async Task ProcessWebhook_Throws_Exception_When_OperationId_Is_Null_Or_Empty_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            WebhookPayload payload = CreateWebhook(WebhookAction.Unsubscribe);
            payload.OperationId = Guid.Empty;

            await Assert.ThrowsAsync<FormatException>(async () => await webhookProcessor.ProcessWebhookAsync(payload));
        }

        [Fact]
        public async Task Calls_authentication_api_and_doesnt_call_webhook_handler_if_authentication_fails_throws_exception_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.Unsubscribe);

            webhookProcessor.WebhookMarketplaceCallerMock.Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>())).ThrowsAsync(new MarketplaceException());

            var webhook = new WebhookProcessor(webhookProcessor.WebhookMarketplaceCallerMock.Object, webhookProcessor.WebhookHandlerMock.Object, webhookProcessor.Logger);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await webhook.ProcessWebhookNotificationAsync(payload));

            webhookProcessor.VerifyGetOperationsIsCalled(payload);
            webhookProcessor.VerifyNoOtherFulfillmentCalls();
            webhookProcessor.VerifyNoOtherWebhookHandlerCalls();
        }

        [Fact]
        public async Task If_authentication_is_successful_and_handler_returns_success_on_delete_then_updates_operation_status_throws_Exception_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.Unsubscribe);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessDeleteAsync(payload)).ReturnsAsync(OperationUpdateStatus.Success);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.UpdateMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, new OperationUpdate(payload.PlanId, payload.Quantity, OperationUpdateStatus.Success), It.IsAny<CancellationToken>()))
                .Throws<MarketplaceException>();

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerDeleteIsCalled();
            webhookProcessor.VerifyUpdateOperationsNotCalled(payload, OperationUpdateStatus.Success);
        }

        [Fact]
        public async Task If_authentication_is_successful_and_handler_returns_success_on_delete_then_updates_operation_status_with_success_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.Unsubscribe);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessDeleteAsync(payload)).ReturnsAsync(OperationUpdateStatus.Success);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerDeleteIsCalled();
            webhookProcessor.VerifyUpdateOperationsNotCalled(payload, OperationUpdateStatus.Success);
        }

        [Fact]
        public async Task If_authentication_is_successful_and_handler_returns_failure_on_delete_then_updates_operation_status_with_failure_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.Unsubscribe);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessDeleteAsync(payload)).ReturnsAsync(OperationUpdateStatus.Failure);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerDeleteIsCalled();
            webhookProcessor.VerifyUpdateOperationsNotCalled(payload, OperationUpdateStatus.Success);
        }

        [Fact]
        public async Task If_authentication_is_successful_and_handler_returns_success_on_suspend_then_updates_operation_status_with_success_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.Suspend);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessSuspendAsync(payload)).ReturnsAsync(OperationUpdateStatus.Success);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerSuspendIsCalled();
            webhookProcessor.VerifyUpdateOperationsNotCalled(payload, OperationUpdateStatus.Success);
        }

        [Fact]
        public async Task If_authentication_is_successful_and_handler_returns_failure_on_suspend_then_updates_operation_status_with_failure_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.Suspend);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessSuspendAsync(payload)).ReturnsAsync(OperationUpdateStatus.Failure);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerSuspendIsCalled();
            webhookProcessor.VerifyUpdateOperationsNotCalled(payload, OperationUpdateStatus.Failure);
        }

        [Fact]
        public async Task If_authentication_is_successful_and_handler_returns_success_on_reinstate_then_updates_operation_status_with_success_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.Reinstate);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessReinstateAsync(payload)).ReturnsAsync(OperationUpdateStatus.Success);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerReinstateIsCalled();
            webhookProcessor.VerifyUpdateOperationsCalled(payload, OperationUpdateStatus.Success);
        }

        [Fact]
        public async Task If_authentication_is_successful_and_handler_returns_failure_on_reinstate_then_updates_operation_status_with_failure_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.Reinstate);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessReinstateAsync(payload)).ReturnsAsync(OperationUpdateStatus.Failure);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerReinstateIsCalled();
            webhookProcessor.VerifyUpdateOperationsCalled(payload, OperationUpdateStatus.Failure);
        }

        [Fact]
        public async Task If_authentication_is_successful_and_handler_returns_success_on_changeplan_then_updates_operation_status_with_success_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.ChangePlan);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessChangePlanAsync(payload)).ReturnsAsync(OperationUpdateStatus.Success);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerChangePlanIsCalled();
            webhookProcessor.VerifyUpdateOperationsCalled(payload, OperationUpdateStatus.Success);
        }

        [Fact]
        public async Task If_authentication_is_successful_and_handler_returns_failure_on_changeplan_then_updates_operation_status_with_failure_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.ChangePlan);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessChangePlanAsync(payload)).ReturnsAsync(OperationUpdateStatus.Failure);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerChangePlanIsCalled();
            webhookProcessor.VerifyUpdateOperationsCalled(payload, OperationUpdateStatus.Failure);
        }

        [Fact]
        public async Task If_authentication_is_successful_and_handler_returns_success_on_changequantity_then_updates_operation_status_with_success_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.ChangeQuantity);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessChangeQuantityAsync(payload)).ReturnsAsync(OperationUpdateStatus.Success);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerChangeQuantityIsCalled();
            webhookProcessor.VerifyUpdateOperationsCalled(payload, OperationUpdateStatus.Success);
        }

        [Fact]
        public async Task If_authentication_is_successful_and_handler_returns_failure_on_changequantity_then_updates_operation_status_with_failure_Async()
        {
            var webhookProcessor = new TestWebhookProcessor();
            var payload = CreateWebhook(WebhookAction.ChangeQuantity);

            webhookProcessor.WebhookMarketplaceCallerMock
                .Setup(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionOperation());

            webhookProcessor.WebhookHandlerMock.Setup(handler => handler.ProcessChangeQuantityAsync(payload)).ReturnsAsync(OperationUpdateStatus.Failure);

            await webhookProcessor.ProcessWebhookAsync(payload);

            webhookProcessor.VerifyWebhookHandlerChangeQuantityIsCalled();
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
            WebhookMarketplaceCallerMock = new Mock<IWebhookMarketplaceCaller>();
            WebhookHandlerMock = new Mock<IWebhookHandler>();
            Logger = new Mock<ILogger>().Object;
        }

        public Mock<IWebhookMarketplaceCaller> WebhookMarketplaceCallerMock { get; }

        public Mock<IWebhookHandler> WebhookHandlerMock { get; }

        public ILogger Logger { get; }

        public async Task ProcessWebhookAsync(WebhookPayload payload)
        {
            var webhookProcessor = new WebhookProcessor(WebhookMarketplaceCallerMock.Object, WebhookHandlerMock.Object, Logger);
            await webhookProcessor.ProcessWebhookNotificationAsync(payload);
        }

        public void VerifyWebhookHandlerDeleteIsCalled()
        {
            WebhookHandlerMock.Verify(handler => handler.ProcessDeleteAsync(It.IsAny<WebhookPayload>()), Times.Once());
            VerifyNoOtherWebhookHandlerCalls();
        }

        public void VerifyWebhookHandlerSuspendIsCalled()
        {
            WebhookHandlerMock.Verify(handler => handler.ProcessSuspendAsync(It.IsAny<WebhookPayload>()), Times.Once());
            VerifyNoOtherWebhookHandlerCalls();
        }

        public void VerifyWebhookHandlerReinstateIsCalled()
        {
            WebhookHandlerMock.Verify(handler => handler.ProcessReinstateAsync(It.IsAny<WebhookPayload>()), Times.Once());
            VerifyNoOtherWebhookHandlerCalls();
        }

        public void VerifyWebhookHandlerChangePlanIsCalled()
        {
            WebhookHandlerMock.Verify(handler => handler.ProcessChangePlanAsync(It.IsAny<WebhookPayload>()), Times.Once());
            VerifyNoOtherWebhookHandlerCalls();
        }

        public void VerifyWebhookHandlerChangeQuantityIsCalled()
        {
            WebhookHandlerMock.Verify(handler => handler.ProcessChangeQuantityAsync(It.IsAny<WebhookPayload>()), Times.Once());
            VerifyNoOtherWebhookHandlerCalls();
        }

        public void VerifyGetOperationsIsCalled(WebhookPayload payload)
        {
            WebhookMarketplaceCallerMock.Verify(client => client.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, It.IsAny<CancellationToken>()), Times.Once());
        }

        public void VerifyUpdateOperationsCalled(WebhookPayload payload, OperationUpdateStatus operationUpdateStatus)
        {
            WebhookMarketplaceCallerMock.Verify(
                client => client.UpdateMarketplaceAsync(
                    payload.MarketplaceSubscription,
                    payload.OperationId,
                    It.Is<OperationUpdate>(update => update.Status == operationUpdateStatus),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        public void VerifyUpdateOperationsNotCalled(WebhookPayload payload, OperationUpdateStatus operationUpdateStatus)
        {
            WebhookMarketplaceCallerMock.Verify(
                client => client.UpdateMarketplaceAsync(
                    payload.MarketplaceSubscription,
                    payload.OperationId,
                    It.Is<OperationUpdate>(update => update.Status == operationUpdateStatus),
                    It.IsAny<CancellationToken>()),
                Times.Never());
        }

        public void VerifyNoOtherFulfillmentCalls()
        {
            WebhookMarketplaceCallerMock.VerifyNoOtherCalls();
        }

        public void VerifyNoOtherWebhookHandlerCalls()
        {
            WebhookHandlerMock.VerifyNoOtherCalls();
        }
    }
}
