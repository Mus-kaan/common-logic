//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace Microsoft.Liftr
{
    public sealed class JenkinsTestResourceGroupScope : TestResourceGroupScope
    {
        public JenkinsTestResourceGroupScope(string resourceGroupName, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
            : base(resourceGroupName, filePath, memberName)
        {
            AzFactory = new LiftrAzureFactory(Logger, JenkinsTestCredentials.TenantId, JenkinsTestCredentials.ObjectId, JenkinsTestCredentials.SubscriptionId, JenkinsTestCredentials.TokenCredential, JenkinsTestCredentials.GetAzureCredentials);
        }

        public JenkinsTestResourceGroupScope(string baseName, ITestOutputHelper output, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
            : base(baseName, output, filePath, memberName)
        {
            AzFactory = new LiftrAzureFactory(Logger, JenkinsTestCredentials.TenantId, JenkinsTestCredentials.ObjectId, JenkinsTestCredentials.SubscriptionId, JenkinsTestCredentials.TokenCredential, JenkinsTestCredentials.GetAzureCredentials);
        }

        public JenkinsTestResourceGroupScope(string baseName, NamingContext namingContext, ITestOutputHelper output, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
            : base(baseName, namingContext, output, filePath, memberName)
        {
            AzFactory = new LiftrAzureFactory(Logger, JenkinsTestCredentials.TenantId, JenkinsTestCredentials.ObjectId, JenkinsTestCredentials.SubscriptionId, JenkinsTestCredentials.TokenCredential, JenkinsTestCredentials.GetAzureCredentials);
        }
    }
}
