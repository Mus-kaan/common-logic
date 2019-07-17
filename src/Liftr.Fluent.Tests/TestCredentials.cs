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
    public static class TestCredentials
    {
        public const string ClientId = "5a6071dc-04c0-455f-bacb-d42c7bca5b2f";

        public static string ClientSecret => "NGY3ZjdlOTgtMGMxMC00YzcwLWIyMGYtNGI3NzczZmIxNTYz".FromBase64();

        public static Azure.Management.Fluent.IAzure GetAzure()
        {
            var subscriptionId = "d8f298fb-60f5-4676-a7d3-25442ec5ce1e";
            var tenantId = "33e01921-4d64-4f8c-a055-5bdaffd5e33d";

            ServiceClientTracing.AddTracingInterceptor(new TracingInterceptor(LoggerFactory.ConsoleLogger));
            ServiceClientTracing.IsEnabled = true;

            var credentials = new AzureCredentialsFactory().FromServicePrincipal(ClientId, ClientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
            var azure = Azure.Management.Fluent.Azure
                            .Configure()
                            .WithDelegatingHandler(new HttpLoggingDelegatingHandler())
                            .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                            .Authenticate(credentials)
                            .WithSubscription(subscriptionId);

            return azure;
        }
    }
}
