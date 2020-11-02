//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent;
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
    }
}
