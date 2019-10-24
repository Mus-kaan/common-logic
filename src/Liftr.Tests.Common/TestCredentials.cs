//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using System;

namespace Microsoft.Liftr
{
    public static class TestCredentials
    {
        // TODO: figure out a better way to access the credentials in CDPx.
        // This is the auth file for a totally separated subscription. It only contains unit test related stuffs.
        private const string c_b64AuthFileContent = "ewogICJjbGllbnRJZCI6ICJjZDk4ZjBkZS04MjAzLTQyY2MtYjUxYi1lZTEyZTQ1MjQ3ZjMiLAogICJjbGllbnRTZWNyZXQiOiAiMDA0NTlkZGUtMGQ5ZS00ZTgzLTk4Y2QtOTQzZGViYWNhZjRiIiwKICAic3Vic2NyaXB0aW9uSWQiOiAiNjBmYWQzNWItM2E0Ny00Y2EwLWI2OTEtNGE3ODlmNzM3Y2VhIiwKICAidGVuYW50SWQiOiAiMzNlMDE5MjEtNGQ2NC00ZjhjLWEwNTUtNWJkYWZmZDVlMzNkIiwKICAiYWN0aXZlRGlyZWN0b3J5RW5kcG9pbnRVcmwiOiAiaHR0cHM6Ly9sb2dpbi5taWNyb3NvZnRvbmxpbmUuY29tIiwKICAicmVzb3VyY2VNYW5hZ2VyRW5kcG9pbnRVcmwiOiAiaHR0cHM6Ly9tYW5hZ2VtZW50LmF6dXJlLmNvbS8iLAogICJhY3RpdmVEaXJlY3RvcnlHcmFwaFJlc291cmNlSWQiOiAiaHR0cHM6Ly9ncmFwaC53aW5kb3dzLm5ldC8iLAogICJzcWxNYW5hZ2VtZW50RW5kcG9pbnRVcmwiOiAiaHR0cHM6Ly9tYW5hZ2VtZW50LmNvcmUud2luZG93cy5uZXQ6ODQ0My8iLAogICJnYWxsZXJ5RW5kcG9pbnRVcmwiOiAiaHR0cHM6Ly9nYWxsZXJ5LmF6dXJlLmNvbS8iLAogICJtYW5hZ2VtZW50RW5kcG9pbnRVcmwiOiAiaHR0cHM6Ly9tYW5hZ2VtZW50LmNvcmUud2luZG93cy5uZXQvIiwKICAiU2VydmljZVByaW5jaXBhbE9iamVjdElkIjogImJkNTcxNDk0LTE3YjAtNGFjMS1hYmI3LTllYjY0YjY0ZTE0NSIKfQ==";

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

        public static string SubscriptionId
        {
            get
            {
                return AuthFileContract.SubscriptionId;
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
