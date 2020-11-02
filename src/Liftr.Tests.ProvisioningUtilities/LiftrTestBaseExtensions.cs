//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.KeyVault;
using System;

namespace Microsoft.Liftr.Tests
{
    public static class LiftrTestBaseExtensions
    {
        public static LiftrAzureFactory GetLiftrAzureFactory(this LiftrTestBase liftrTestBase, string subscriptionId = null)
        {
            if (liftrTestBase == null)
            {
                throw new ArgumentNullException(nameof(liftrTestBase));
            }

            if (string.IsNullOrEmpty(subscriptionId))
            {
                subscriptionId = JenkinsTestCredentials.SubscriptionId;
            }

            return new LiftrAzureFactory(liftrTestBase.Logger, JenkinsTestCredentials.TenantId, JenkinsTestCredentials.ObjectId, subscriptionId, JenkinsTestCredentials.TokenCredential, JenkinsTestCredentials.GetAzureCredentials);
        }

        public static KeyVaultClient GetKeyVaultClient(this LiftrTestBase liftrTestBase)
        {
            if (liftrTestBase == null)
            {
                throw new ArgumentNullException(nameof(liftrTestBase));
            }

            return KeyVaultClientFactory.FromClientIdAndSecret(JenkinsTestCredentials.ClientId, JenkinsTestCredentials.ClientSecret);
        }
    }
}
