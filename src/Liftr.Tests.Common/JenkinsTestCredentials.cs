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
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr
{
    public static class JenkinsTestCredentials
    {
        private const string LIFTR_CICD_AUTH_FILE_BASE64 = nameof(LIFTR_CICD_AUTH_FILE_BASE64);
        private const string LIFTR_CICD_TEST_SUBSCRIPTION_LIST = nameof(LIFTR_CICD_TEST_SUBSCRIPTION_LIST);

        private static readonly List<string> s_subscriptionList;
        private static readonly Random s_rand = new Random();

#pragma warning disable CA1810 // Initialize reference type static fields inline
        static JenkinsTestCredentials()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            var encodedAuthFile = Environment.GetEnvironmentVariable(LIFTR_CICD_AUTH_FILE_BASE64);
            if (!string.IsNullOrEmpty(encodedAuthFile))
            {
                AuthFileContract = AuthFileContract.FromFileContent(encodedAuthFile.FromBase64());
            }

            var subscriptionListStr = Environment.GetEnvironmentVariable(LIFTR_CICD_TEST_SUBSCRIPTION_LIST);
            if (!string.IsNullOrEmpty(subscriptionListStr))
            {
                s_subscriptionList = subscriptionListStr.Split(',').ToList();
            }
            else
            {
                s_subscriptionList = new List<string>();
            }
        }

        public static AuthFileContract AuthFileContract { get; }

        public static Func<AzureCredentials> GetAzureCredentials
        {
            get
            {
                Check();
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
                Check();
                return KeyVaultClientFactory.FromClientIdAndSecret(AuthFileContract.ClientId, AuthFileContract.ClientSecret);
            }
        }

        public static ClientSecretCredential TokenCredential
        {
            get
            {
                Check();
                return new ClientSecretCredential(TestCredentials.TenantId, TestCredentials.ClientId, TestCredentials.ClientSecret);
            }
        }

        public static string SubscriptionId
        {
            get
            {
                Check();
                if (s_subscriptionList.Any())
                {
                    var idx = s_rand.Next(0, s_subscriptionList.Count);
                    return s_subscriptionList[idx];
                }

                return AuthFileContract.SubscriptionId;
            }
        }

        public static string TenantId
        {
            get
            {
                Check();
                return AuthFileContract.TenantId;
            }
        }

        public static string ClientId
        {
            get
            {
                Check();
                return AuthFileContract.ClientId;
            }
        }

        public static string ClientSecret
        {
            get
            {
                Check();
                return AuthFileContract.ClientSecret;
            }
        }

        public static string ObjectId
        {
            get
            {
                Check();
                return AuthFileContract.ServicePrincipalObjectId;
            }
        }

        private static void Check()
        {
            if (AuthFileContract == null || string.IsNullOrEmpty(AuthFileContract.ClientSecret))
            {
                throw new InvalidOperationException($"Cannot load the test credentials auth file from environment variable '{LIFTR_CICD_AUTH_FILE_BASE64}'");
            }
        }
    }
}
