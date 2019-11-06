//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Microsoft.Liftr.Fluent
{
    public interface ILiftrAzureFactory
    {
        ILiftrAzure GenerateLiftrAzure(string subscriptionId = null, HttpLoggingDelegatingHandler.Level logLevel = HttpLoggingDelegatingHandler.Level.Basic);
    }
}
