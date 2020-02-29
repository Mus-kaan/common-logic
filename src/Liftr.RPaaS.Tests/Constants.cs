//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.RPaaS.Tests
{
    public static class Constants
    {
        public const string UserRpSubscriptionId = "f9aed45d-b9e6-462a-a3f5-6ab34857bc17";
        public const string ProviderNamespace = "Microsoft.Nginx";
        public const string ResourceType = "frontend";
        public const string ApiVersion = "2019-11-01-preview";
        public const string Expand = "crossPartitionQuery";
        public const string MetaRpEndpoint = "https://metarp.com";
        public const string NextLink = MetaRpEndpoint + "/nextLink";
        public const string RequestPath = "/subscriptions/" + UserRpSubscriptionId + "/providers/" + ProviderNamespace + "/" + ResourceType;
        public const string RequestEndpoint = MetaRpEndpoint + RequestPath;
        public const string FullEndpoint = RequestEndpoint + "?api-version=" + ApiVersion + "&$expand=" + Expand;

        public static TestResource Resource1()
        {
            return new TestResource()
            {
                Type = "Microsoft.Nginx/frontends",
                Id = "/subscriptions/f9aed45d-b9e6-462a-a3f5-6ab34857bc17/resourceGroups/myrg/providers/Microsoft.Nginx/frontends/frontend",
                Name = "frontend",
                Location = "eastus",
            };
        }

        public static TestResource Resource2()
        {
            return new TestResource()
            {
                Type = "Microsoft.Nginx/frontends",
                Id = "/subscriptions/f9aed45d-b9e6-462a-a3f5-6ab34857bc17/resourceGroups/myrg/providers/Microsoft.Nginx/frontends/frontend2",
                Name = "frontend2",
                Location = "eastus",
            };
        }
    }
}
