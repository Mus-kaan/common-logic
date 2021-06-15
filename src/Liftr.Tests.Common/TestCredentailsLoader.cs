//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Identity;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Tests
{
    public static class TestCredentailsLoader
    {
        private static readonly Dictionary<CloudType, string> s_authEnv = new Dictionary<CloudType, string>()
        {
            [CloudType.DogFood] = "LIFTR_UNIT_TEST_AUTH_FILE_BASE64_DOGFOOD",
            [CloudType.Public] = "LIFTR_UNIT_TEST_AUTH_FILE_BASE64",
        };

        private static readonly Dictionary<CloudType, AuthFileContract> s_cachedAuths = new Dictionary<CloudType, AuthFileContract>();

        public static AuthFileContract LoadAuthFileContract(CloudType cloud)
        {
            lock (s_cachedAuths)
            {
                if (!s_cachedAuths.ContainsKey(cloud))
                {
                    if (!s_authEnv.ContainsKey(cloud))
                    {
                        throw new InvalidOperationException($"Do not know which environment variable to use for loading the credentials for '{cloud}'");
                    }

                    var envName = s_authEnv[cloud];
                    var encodedAuthFile = Environment.GetEnvironmentVariable(envName);
                    if (string.IsNullOrEmpty(encodedAuthFile))
                    {
                        throw new InvalidOperationException($"Cannot find the credential for running the unit tests. It should be set in the environment variable with name {envName}. Details: https://aka.ms/liftr-test-cred");
                    }

                    s_cachedAuths[cloud] = AuthFileContract.FromFileContent(encodedAuthFile.FromBase64());
                }

                return s_cachedAuths[cloud];
            }
        }

        public static TestCredentails LoadTestCredentails(CloudType cloud, Serilog.ILogger logger)
        {
            var authFile = LoadAuthFileContract(cloud);

            AzureEnvironment azEnv = null;
            if (cloud == CloudType.Public)
            {
                azEnv = AzureEnvironment.AzureGlobalCloud;
            }
            else if (cloud == CloudType.DogFood)
            {
                azEnv = new AzureEnvironment()
                {
                    Name = "AzureDogFood",
                    AuthenticationEndpoint = "https://login.windows-ppe.net/",
                    ResourceManagerEndpoint = "https://api-dogfood.resources.windows-int.net/",
                    ManagementEndpoint = "https://management.core.windows.net/", // TODO: figure this out
                    GraphEndpoint = "https://graph.ppe.windows.net/",
                    StorageEndpointSuffix = "core.windows.net", // TODO: figure this out
                    KeyVaultSuffix = "vault-int.azure-int.net",
                };
            }
            else
            {
                throw new InvalidOperationException($"Do not know the Azure endpoint for cloud {cloud}");
            }

            var options = new TokenCredentialOptions()
            {
                AuthorityHost = new Uri(azEnv.AuthenticationEndpoint),
            };
            var clientSecretCredential = new ClientSecretCredential(authFile.TenantId, authFile.ClientId, authFile.ClientSecret, options);

            Func<AzureCredentials> getAzureCredentials = () =>
            {
                return SdkContext.AzureCredentialsFactory
                        .FromServicePrincipal(authFile.ClientId, authFile.ClientSecret, authFile.TenantId, azEnv);
            };

            var azFactory = new LiftrAzureFactory(logger, authFile.TenantId, authFile.ServicePrincipalObjectId, authFile.SubscriptionId, clientSecretCredential, getAzureCredentials);

            var credentails = new TestCredentails()
            {
                AzFactory = azFactory,
                TokenCrendetial = clientSecretCredential,
                AzureCredentialsProvider = getAzureCredentials,
                SubscriptionId = authFile.SubscriptionId,
            };

            return credentails;
        }
    }

    public class TestCredentails
    {
        public LiftrAzureFactory AzFactory { get; set; }

        public ClientSecretCredential TokenCrendetial { get; set; }

        public Func<AzureCredentials> AzureCredentialsProvider { get; set; }

        public string SubscriptionId { get; set; }
    }
}
