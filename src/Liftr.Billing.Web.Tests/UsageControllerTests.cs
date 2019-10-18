//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Billing.Web.Tests
{
    public class UsageControllerTests
    {
        [Fact]
        public async Task When_event_is_posted_it_returns_success_Async()
        {
            var usageEvent = new UsageEvent()
            {
                SubscriptionId = Guid.Parse("8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64"),
                EventId = Guid.NewGuid(),
                MeterId = "meterId",
                EventDateTime = DateTime.Now,
                Location = "westus",
                Quantity = 100,
                ResourceUri = "/subscriptions/8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64/resourceGroups/Flying-SuperMan-Group/providers/Microsoft.ClassicCompute/VirtualMachines/vm0​",
            };

            using (var billingService = new BillingService())
            {
                using (var request = GetPostRequestMessage(usageEvent))
                {
                    var response = await billingService.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task When_event_is_posted_it_is_added_to_table_Async()
        {
            var usageEvent = new UsageEvent()
            {
                SubscriptionId = Guid.Parse("8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64"),
                EventId = Guid.NewGuid(),
                MeterId = "meterId",
                EventDateTime = DateTime.Now,
                Location = "westus",
                Quantity = 100,
                ResourceUri = "/subscriptions/8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64/resourceGroups/Flying-SuperMan-Group/providers/Microsoft.ClassicCompute/VirtualMachines/vm0​",
            };

            using (var billingService = new BillingService())
            {
                using (var request = GetPostRequestMessage(usageEvent))
                {
                    await billingService.SendAsync(request);
                    Assert.Single(billingService.BillingServiceProvider.UsageTable.UsageRecordsList);
                    var insertedRecord = billingService.BillingServiceProvider.UsageTable.UsageRecordsList.Single();
                    Assert.Equal(insertedRecord.SubscriptionId, usageEvent.SubscriptionId);
                    Assert.Equal(insertedRecord.EventId, usageEvent.EventId);
                    Assert.Equal(insertedRecord.MeterId, usageEvent.MeterId);
                    Assert.Equal(insertedRecord.EventDateTime, usageEvent.EventDateTime.ToUniversalTime());
                    Assert.Equal(insertedRecord.ResourceUri, usageEvent.ResourceUri);
                }
            }
        }

        [Fact]
        public async Task When_event_is_posted_it_is_added_to_queue_Async()
        {
            var usageEvent = new UsageEvent()
            {
                SubscriptionId = Guid.Parse("8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64"),
                EventId = Guid.NewGuid(),
                MeterId = "meterId",
                EventDateTime = DateTime.Now,
                Location = "westus",
                Quantity = 100,
                ResourceUri = "/subscriptions/8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64/resourceGroups/Flying-SuperMan-Group/providers/Microsoft.ClassicCompute/VirtualMachines/vm0​",
            };

            using (var billingService = new BillingService())
            {
                using (var request = GetPostRequestMessage(usageEvent))
                {
                    await billingService.SendAsync(request);
                    Assert.Single(billingService.BillingServiceProvider.UsageQueue.Messages);
                }
            }
        }

        [Fact]
        public async Task When_event_with_null_subscriptionid_is_posted_it_returns_400_Async()
        {
            var usageEvent = new UsageEvent()
            {
                EventId = Guid.NewGuid(),
                MeterId = "meterId",
                EventDateTime = DateTime.Now,
                Location = "westus",
                Quantity = 100,
                ResourceUri = "/subscriptions/8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64/resourceGroups/Flying-SuperMan-Group/providers/Microsoft.ClassicCompute/VirtualMachines/vm0​",
            };

            using (var billingService = new BillingService())
            {
                using (var request = GetPostRequestMessage(usageEvent))
                {
                    var response = await billingService.SendAsync(request);
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task When_an_event_is_posted_it_adds_same_event_to_queue_and_table_Async()
        {
            var usageEvent = new UsageEvent()
            {
                SubscriptionId = Guid.Parse("8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64"),
                EventId = Guid.NewGuid(),
                MeterId = "meterId",
                EventDateTime = DateTime.Now,
                Location = "westus",
                Quantity = 100,
                ResourceUri = "/subscriptions/8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64/resourceGroups/Flying-SuperMan-Group/providers/Microsoft.ClassicCompute/VirtualMachines/vm0​",
            };

            using (var billingService = new BillingService())
            {
                using (var request = GetPostRequestMessage(usageEvent))
                {
                    var response = await billingService.SendAsync(request);
                    var queueMessage = billingService.BillingServiceProvider.UsageQueue.Messages.Single();
                    var tableEntity = billingService.BillingServiceProvider.UsageTable.UsageRecordsList.Single();
                    Assert.Equal(tableEntity.PartitionKey, queueMessage.FromJson<PushAgentUsageQueueMessage>().PartitionKey);
                }
            }
        }

        private HttpRequestMessage GetPostRequestMessage(UsageEvent usageEvent)
        {
            return new HttpRequestMessage(HttpMethod.Post, "/api/usageEvent")
            {
                Content = new StringContent(usageEvent.ToJson(), Encoding.UTF8, "application/json"),
            };
        }
    }
}
