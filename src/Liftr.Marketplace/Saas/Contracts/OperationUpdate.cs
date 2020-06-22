//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Marketplace.Saas.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OperationUpdateStatus
    {
        Success,
        Failure,
    }

    /// <summary>
    /// Used to update the status of a pending operation
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#update-the-status-of-an-operation
    /// </summary>
    public class OperationUpdate
    {
        public OperationUpdate(string planId, int quantity, OperationUpdateStatus status)
        {
            PlanId = planId;
            Quantity = quantity;
            Status = status;
        }

        public string PlanId { get; set; }

        public int Quantity { get; set; }

        public OperationUpdateStatus Status { get; set; }
    }
}
