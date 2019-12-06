//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.ARM;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.RPaaS
{
    /// <summary>
    /// RP metadata resource pool storage client accessor
    /// </summary>
    public interface IMetaRPStorageClient
    {
        /// <summary>
        /// Retrieves ARM resource for given resource id and api version from MetaRP
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<T> GetResourceAsync<T>(string resourceId, string apiVersion) where T : ARMResource;

        /// <summary>
        /// Updates ARM resource for given api version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resource"></param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> UpdateResourceAsync<T>(T resource, string apiVersion) where T : ARMResource;
    }
}
