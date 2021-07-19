//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public interface ILiftrAzureFactory
    {
        string TenantId { get; }

        ILiftrAzure GenerateLiftrAzure(string subscriptionId = null, HttpLoggingDelegatingHandler.Level logLevel = HttpLoggingDelegatingHandler.Level.Basic);

        Task<string> GetStorageConnectionStringAsync(Liftr.Contracts.ResourceId resourceId);
    }
}
