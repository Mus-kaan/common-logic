//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
        private readonly UsageEvent _sampleUsageEvent1 = new UsageEvent()
        {
            SubscriptionId = Guid.Parse("8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64"),
            EventId = Guid.NewGuid(),
            MeterId = "meterId",
            EventDateTime = DateTime.Now,
            Location = "westus",
            Quantity = 100,
            ResourceUri = "/subscriptions/8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64/resourceGroups/Flying-SuperMan-Group/providers/Microsoft.ClassicCompute/VirtualMachines/vm0​",
        };

        [Fact]
        public async Task When_event_is_posted_it_returns_success_Async()
        {
            using (var billingService = new BillingService())
            {
                using (var request = GetSingleUsageRequest(_sampleUsageEvent1))
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
            using (var billingService = new BillingService())
            {
                using (var request = GetSingleUsageRequest(_sampleUsageEvent1))
                {
                    await billingService.SendAsync(request);
                    Assert.Single(billingService.BillingServiceProvider.UsageTable.UsageRecordsList);
                    var insertedRecord = billingService.BillingServiceProvider.UsageTable.UsageRecordsList.Single();
                    Assert.Equal(insertedRecord.SubscriptionId, _sampleUsageEvent1.SubscriptionId);
                    Assert.Equal(insertedRecord.EventId, _sampleUsageEvent1.EventId);
                    Assert.Equal(insertedRecord.MeterId, _sampleUsageEvent1.MeterId);
                    Assert.Equal(insertedRecord.EventDateTime, _sampleUsageEvent1.EventDateTime.ToUniversalTime());
                    Assert.Equal(insertedRecord.ResourceUri, _sampleUsageEvent1.ResourceUri);
                }
            }
        }

        [Fact]
        public async Task When_event_is_posted_it_is_added_to_queue_Async()
        {
            using (var billingService = new BillingService())
            {
                using (var request = GetSingleUsageRequest(_sampleUsageEvent1))
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
                using (var request = GetSingleUsageRequest(usageEvent))
                {
                    var response = await billingService.SendAsync(request);
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task When_an_event_is_posted_it_adds_same_event_to_queue_and_table_Async()
        {
            using (var billingService = new BillingService())
            {
                using (var request = GetSingleUsageRequest(_sampleUsageEvent1))
                {
                    var response = await billingService.SendAsync(request);
                    var queueMessage = billingService.BillingServiceProvider.UsageQueue.Messages.Single();
                    var tableEntity = billingService.BillingServiceProvider.UsageTable.UsageRecordsList.Single();
                    Assert.Equal(tableEntity.PartitionKey, queueMessage.FromJson<PushAgentUsageQueueMessage>().PartitionKey);
                }
            }
        }

        [Fact]
        public async Task When_batch_event_is_posted_it_returns_success_Async()
        {
            var batchUsage = new BatchUsageEvent
            {
                UsageEvents = new List<UsageEvent>()
                {
                    _sampleUsageEvent1,
                },
            };

            using (var billingService = new BillingService())
            {
                using (var request = GetBatchUsageRequest(batchUsage))
                {
                    var response = await billingService.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task When_batch_event_is_posted_it_is_added_to_table_Async()
        {
            var sampleUsageEvent2 = new UsageEvent()
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
                var batchUsage = new BatchUsageEvent
                {
                    UsageEvents = new List<UsageEvent>()
                    {
                        _sampleUsageEvent1,
                        sampleUsageEvent2,
                    },
                };

                using (var request = GetBatchUsageRequest(batchUsage))
                {
                    await billingService.SendAsync(request);
                    Assert.Equal(2, billingService.BillingServiceProvider.UsageTable.UsageRecordsList.Count);
                    var firstRecord = billingService.BillingServiceProvider.UsageTable.UsageRecordsList.First();
                    Assert.Equal(firstRecord.EventId, _sampleUsageEvent1.EventId);
                    var secondRecord = billingService.BillingServiceProvider.UsageTable.UsageRecordsList.ElementAt(1);
                    Assert.Equal(secondRecord.EventId, sampleUsageEvent2.EventId);
                }
            }
        }

        [Fact]
        public async Task When_batch_of_more_than_max_size_is_inserted_it_returns_400_Async()
        {
            var sampleUsageEvent2 = new UsageEvent()
            {
                SubscriptionId = Guid.Parse("8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64"),
                EventId = Guid.NewGuid(),
                MeterId = "meterId",
                EventDateTime = DateTime.Now,
                Location = "westus",
                Quantity = 100,
                ResourceUri = "/subscriptions/8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64/resourceGroups/Flying-SuperMan-Group/providers/Microsoft.ClassicCompute/VirtualMachines/vm0​",
            };

            var usageEvents = new List<UsageEvent>();
            for (int i = 1; i <= TableConstants.TableServiceBatchMaximumOperations + 1; i++)
            {
                usageEvents.Add(new UsageEvent()
                {
                    SubscriptionId = Guid.NewGuid(),
                    EventId = Guid.NewGuid(),
                    MeterId = "meterId",
                    EventDateTime = DateTime.Now,
                    Location = "westus",
                    Quantity = 100,
                    ResourceUri = "/subscriptions/8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64/resourceGroups/Flying-SuperMan-Group/providers/Microsoft.ClassicCompute/VirtualMachines/vm0​",
                });
            }

            using (var billingService = new BillingService())
            {
                var batchUsage = new BatchUsageEvent
                {
                    UsageEvents = usageEvents,
                };

                using (var request = GetBatchUsageRequest(batchUsage))
                {
                    var response = await billingService.SendAsync(request);
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }

        private HttpRequestMessage GetBatchUsageRequest(BatchUsageEvent batchUsage)
        {
            return new HttpRequestMessage(HttpMethod.Post, "/api/batchUsageEvent")
            {
                Content = new StringContent(batchUsage.ToJson(), Encoding.UTF8, "application/json"),
            };
        }

        private HttpRequestMessage GetSingleUsageRequest(UsageEvent usageEvent)
        {
            return new HttpRequestMessage(HttpMethod.Post, "/api/usageEvent")
            {
                Content = new StringContent(usageEvent.ToJson(), Encoding.UTF8, "application/json"),
            };
        }
    }
}
