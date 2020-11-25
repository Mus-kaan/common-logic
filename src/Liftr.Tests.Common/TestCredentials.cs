//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Identity;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using System;

namespace Microsoft.Liftr
{
    public static class TestCredentials
    {
        public const string SharedKeyVaultResourceId = "/subscriptions/f885cf14-b751-43c1-9536-dc5b1be02bc0/resourceGroups/unit-test-shared-wus2-rg/providers/Microsoft.KeyVault/vaults/tests-shared-wus2-kv";

        public const string SharedKeyVaultUri = "https://tests-shared-wus2-kv.vault.azure.net/";

        // 'liftr-ms-unit-test-only' https://portal.azure.com/?feature.customportal=false#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/72086f7d-aa29-466a-8daa-91a787d1f6e4/isMSAApp/
        private const string LIFTR_UNIT_TEST_AUTH_FILE_BASE64 = nameof(LIFTR_UNIT_TEST_AUTH_FILE_BASE64);

        public static AuthFileContract AuthFileContract
        {
            get
            {
                var encodedAuthFile = Environment.GetEnvironmentVariable(LIFTR_UNIT_TEST_AUTH_FILE_BASE64);
                if (string.IsNullOrEmpty(encodedAuthFile))
                {
                    throw new InvalidOperationException($"Cannot find the credential for running the unit tests. It should be set in the environment variable with name {LIFTR_UNIT_TEST_AUTH_FILE_BASE64}. Details: https://aka.ms/liftr-test-cred");
                }

                return AuthFileContract.FromFileContent(encodedAuthFile.FromBase64());
            }
        }

        public static Func<AzureCredentials> GetAzureCredentials
        {
            get
            {
                return () =>
                {
                    return SdkContext.AzureCredentialsFactory
                        .FromServicePrincipal(AuthFileContract.ClientId, AuthFileContract.ClientSecret, AuthFileContract.TenantId, AzureEnvironment.AzureGlobalCloud);
                };
            }
        }

        public static KeyVaultClient KeyVaultClient
        {
            get
            {
                return KeyVaultClientFactory.FromClientIdAndSecret(AuthFileContract.ClientId, AuthFileContract.ClientSecret);
            }
        }

        public static ClientSecretCredential TokenCredential => new ClientSecretCredential(TestCredentials.TenantId, TestCredentials.ClientId, TestCredentials.ClientSecret);

        public static string SubscriptionId
        {
            get
            {
                return AuthFileContract.SubscriptionId;
            }
        }

        public static string TenantId
        {
            get
            {
                return AuthFileContract.TenantId;
            }
        }

        public static string ClientId
        {
            get
            {
                return AuthFileContract.ClientId;
            }
        }

        public static string ClientSecret
        {
            get
            {
                return AuthFileContract.ClientSecret;
            }
        }

        public static string ObjectId
        {
            get
            {
                return AuthFileContract.ServicePrincipalObjectId;
            }
        }
    }
}
