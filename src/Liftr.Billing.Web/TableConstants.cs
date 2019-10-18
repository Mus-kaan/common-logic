//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Billing.Web
{
    public static class TableConstants
    {
        // The maximum size of the batch operations is defined as 100 here
        // https://docs.microsoft.com/en-us/rest/api/storageservices/performing-entity-group-transactions#table-service-support-for-odata-batch-requests
        // To do: Remove the constant here once the TableConstants class in Microsoft.Azure.Cosmos.Table becomes public
        public const int TableServiceBatchMaximumOperations = 100;
    }
}