//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Liftr.Billing.Web
{
    public class UsageEvent
    {
        // The elements here conform to the push agent schema
        // https://microsoft.sharepoint.com/teams/CustomerAcquisitionBilling/_layouts/15/Doc.aspx?sourcedoc={c7a559f4-316d-46b1-b5d4-f52cdfbc4389}&action=edit&wd=target%28Design.one%7Cff91ebf6-2bd5-4156-a9be-a227cd7576b7%2FUsage%20Record%7C0c2f7a5d-8a81-4ffa-8fe7-db8f51b78a77%2F%29
        [NotEmpty]

        // To do: Confirm if we should have validation for subscription id as mentioned in the push agent documentation
        public Guid SubscriptionId { get; set; }

        [NotEmpty]
        public Guid EventId { get; set; }

        [Required]
        public DateTime EventDateTime { get; set; }

        [Required]
        public double Quantity { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string MeterId { get; set; }

        [Required(AllowEmptyStrings = false)]
#pragma warning disable CA1056 // Uri properties should not be strings
        public string ResourceUri { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

        [Required(AllowEmptyStrings = false)]
        public string Location { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        public Dictionary<string, string> AdditionalInfo { get; set; }

        public string PartNumber { get; set; }

        public string OrderNumber { get; set; }
    }
}
