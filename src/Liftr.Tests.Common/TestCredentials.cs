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

        // TODO: figure out a better way to access the credentials in CDPx.
        // This is the auth file for a totally separated subscription. It only contains unit test related stuffs.
        // 'liftr-ms-unit-test-only' https://portal.azure.com/?feature.customportal=false#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/72086f7d-aa29-466a-8daa-91a787d1f6e4/isMSAApp/
        private const string c_b64AuthFileContent = "ewogICJjbGllbnRJZCI6ICI3MjA4NmY3ZC1hYTI5LTQ2NmEtOGRhYS05MWE3ODdkMWY2ZTQiLAogICJjbGllbnRTZWNyZXQiOiAiN2ExODJhOTItOWMwNS00Mzc2LTk4NWUtOWM4OThkODI5M2Y5IiwKICAic3Vic2NyaXB0aW9uSWQiOiAiZjg4NWNmMTQtYjc1MS00M2MxLTk1MzYtZGM1YjFiZTAyYmMwIiwKICAidGVuYW50SWQiOiAiNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3IiwKICAiYWN0aXZlRGlyZWN0b3J5RW5kcG9pbnRVcmwiOiAiaHR0cHM6Ly9sb2dpbi5taWNyb3NvZnRvbmxpbmUuY29tIiwKICAicmVzb3VyY2VNYW5hZ2VyRW5kcG9pbnRVcmwiOiAiaHR0cHM6Ly9tYW5hZ2VtZW50LmF6dXJlLmNvbS8iLAogICJhY3RpdmVEaXJlY3RvcnlHcmFwaFJlc291cmNlSWQiOiAiaHR0cHM6Ly9ncmFwaC53aW5kb3dzLm5ldC8iLAogICJzcWxNYW5hZ2VtZW50RW5kcG9pbnRVcmwiOiAiaHR0cHM6Ly9tYW5hZ2VtZW50LmNvcmUud2luZG93cy5uZXQ6ODQ0My8iLAogICJnYWxsZXJ5RW5kcG9pbnRVcmwiOiAiaHR0cHM6Ly9nYWxsZXJ5LmF6dXJlLmNvbS8iLAogICJtYW5hZ2VtZW50RW5kcG9pbnRVcmwiOiAiaHR0cHM6Ly9tYW5hZ2VtZW50LmNvcmUud2luZG93cy5uZXQvIiwKICAiU2VydmljZVByaW5jaXBhbE9iamVjdElkIjogIjY0OGY3ZTE4LTU4ODgtNDIzZi04ODZiLWQwOWVkOGEwMDY0ZCIKfQ==";

        static TestCredentials()
        {
            AuthFileContract = AuthFileContract.FromFileContent(c_b64AuthFileContent.FromBase64());
        }

        public static AuthFileContract AuthFileContract { get; }

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
