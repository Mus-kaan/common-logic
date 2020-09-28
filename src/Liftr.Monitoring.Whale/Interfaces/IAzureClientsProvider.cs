//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceGraph;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Whale.Interfaces
{
    /// <summary>
    /// Provider for implementations of Azure clients.
    /// </summary>
    public interface IAzureClientsProvider
    {
        /// <summary>
        /// Get resource graph client for given tenant.
        /// </summary>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        Task<IResourceGraphClient> GetResourceGraphClientAsync(string tenantId);

        /// <summary>
        /// Get Azure fluent client for given subscription.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        Task<IAzure> GetFluentClientAsync(string subscriptionId, string tenantId);
    }
}
