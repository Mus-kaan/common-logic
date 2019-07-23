//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Logging;
using Microsoft.Rest;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class TestCredentials : TestServicePrincipalMS
    {
        public static Azure.Management.Fluent.IAzure GetAzure()
        {
            ServiceClientTracing.AddTracingInterceptor(new TracingInterceptor(LoggerFactory.ConsoleLogger));
            ServiceClientTracing.IsEnabled = true;

            var credentials = new AzureCredentialsFactory().FromServicePrincipal(ClientId, ClientSecret, TenantId, AzureEnvironment.AzureGlobalCloud);
            var azure = Azure.Management.Fluent.Azure
                            .Configure()
                            .WithDelegatingHandler(new HttpLoggingDelegatingHandler())
                            .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                            .Authenticate(credentials)
                            .WithSubscription(SubscriptionId);

            return azure;
        }
    }

#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
    public class TestServicePrincipalAME
#pragma warning restore CA1052 // Static holder types should be Static or NotInheritable
    {
        public const string ClientId = "5a6071dc-04c0-455f-bacb-d42c7bca5b2f";

        public const string SubscriptionId = "d8f298fb-60f5-4676-a7d3-25442ec5ce1e";

        public const string TenantId = "33e01921-4d64-4f8c-a055-5bdaffd5e33d";

        public const string ObjectId = "11f2c714-9364-47dc-a018-fb2ddc0a1a0f";

        public static string ClientSecret => "NGY3ZjdlOTgtMGMxMC00YzcwLWIyMGYtNGI3NzczZmIxNTYz".FromBase64();
    }

#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
    public class TestServicePrincipalMS
#pragma warning restore CA1052 // Static holder types should be Static or NotInheritable
    {
        public const string ClientId = "992e8c63-fd12-4781-a514-1619a5a9a5c0";

        public const string SubscriptionId = "d21a525e-7c86-486d-a79e-a4f3622f639a";

        public const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";

        public const string ObjectId = "1c064670-2e9f-436e-be1b-0f7f40afd0c9";

        public static string ClientSecret => "ZTdjODAzNTYtMjVkYi00NmJmLTkzNGEtMmU4ZWY0YjBhMDcy".FromBase64();
    }
}
