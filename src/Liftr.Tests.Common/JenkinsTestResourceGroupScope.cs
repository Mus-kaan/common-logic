//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace Microsoft.Liftr
{
    public sealed class JenkinsTestResourceGroupScope : TestResourceGroupScope
    {
        public JenkinsTestResourceGroupScope(string baseName, ITestOutputHelper output, EnvironmentType? env = null, string subscriptionId = null, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
            : base(env.HasValue ? $"{env.Value.ShortName()}-{baseName}" : baseName, output, env, filePath, memberName, loadCredentials: false)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                subscriptionId = JenkinsTestCredentials.SubscriptionId;
            }

            AzFactory = new LiftrAzureFactory(Logger, JenkinsTestCredentials.TenantId, JenkinsTestCredentials.ObjectId, subscriptionId, JenkinsTestCredentials.TokenCredential, JenkinsTestCredentials.GetAzureCredentials);
        }
    }
}
