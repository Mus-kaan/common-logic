//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Billing.Web
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "<Pending>")]

    public class UsageRecordEntity
        : TableEntity
    {
        // The contract with the push agent can be found here
        // https://microsoft.sharepoint.com/teams/CustomerAcquisitionBilling/_layouts/15/Doc.aspx?sourcedoc={c7a559f4-316d-46b1-b5d4-f52cdfbc4389}&action=edit&wd=target%28Design.one%7Cff91ebf6-2bd5-4156-a9be-a227cd7576b7%2FUsage%20Record%7C0c2f7a5d-8a81-4ffa-8fe7-db8f51b78a77%2F%29&wdorigin=703
        public UsageRecordEntity()
        {
        }

        public UsageRecordEntity(
            string partitionKey,
            string rowKey,
            Guid subscriptionId,
            Guid eventId,
            DateTime eventDateTime,
            double quantity,
            string meterId,
            string resourceUri,
            Dictionary<string, string> tags,
            string location)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                throw new ArgumentException("Invalid partition key", nameof(partitionKey));
            }

            if (string.IsNullOrEmpty(rowKey))
            {
                throw new ArgumentException("Invalid row key", nameof(rowKey));
            }

            if (string.IsNullOrEmpty(resourceUri))
            {
                throw new ArgumentException("message", nameof(resourceUri));
            }

            PartitionKey = partitionKey;
            RowKey = rowKey;
            SubscriptionId = subscriptionId;
            EventId = eventId;
            EventDateTime = eventDateTime;
            Quantity = Math.Round(quantity, 6);
            MeterId = meterId;
            ResourceUri = resourceUri;
            Tags = tags;
            Location = location;
        }

        public Guid SubscriptionId { get; set; }

        public Guid EventId { get; set; }

        public DateTime EventDateTime { get; set; }

        public double Quantity { get; set; }

        public string MeterId { get; set; }

        public string ResourceUri { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        public string Location { get; set; }

        public Dictionary<string, string> AdditionalInfo { get; set; }

        public string PartNumber { get; set; }

        public string OrderNumber { get; set; }

        public static UsageRecordEntity From(UsageEvent usageEvent, string partitionKey)
        {
            if (usageEvent is null)
            {
                throw new ArgumentNullException(nameof(usageEvent));
            }

            var rowKey = Guid.NewGuid().ToString();

            return new UsageRecordEntity(
                                         partitionKey,
                                         rowKey,
                                         usageEvent.SubscriptionId,
                                         usageEvent.EventId,
                                         usageEvent.EventDateTime.ToUniversalTime(),
                                         usageEvent.Quantity,
                                         usageEvent.MeterId,
                                         usageEvent.ResourceUri,
                                         usageEvent.Tags,
                                         usageEvent.Location);
        }
    }
}