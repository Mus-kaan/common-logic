//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Microsoft.Liftr.Fluent
{
    public interface ILiftrAzureFactory
    {
        ILiftrAzure GenerateLiftrAzure(HttpLoggingDelegatingHandler.Level logLevel = HttpLoggingDelegatingHandler.Level.Basic);
    }
}
