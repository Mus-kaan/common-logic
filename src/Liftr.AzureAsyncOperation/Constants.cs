//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.AzureAsyncOperation
{
    public static class Constants
    {
        public const string AsyncOperationHeader = "Azure-AsyncOperation";

        public const string AsyncOperationLocationFormat = "https://{0}/subscriptions/{1}/providers/{2}/locations/{3}/operationsStatus" + "/{4}?api-version={5}";

        public const string AsyncOperationRoute = "subscriptions/{subscriptionId}/providers/{provider}/locations/{location}/operationsStatus" + "/{operationId}";
    }
}
