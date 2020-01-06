//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.ARM;
using System;
using System.Collections;
using System.Collections.Generic;
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

        /// <summary>
        /// Get all resources of a particular type in the provider namespace
        /// </summary>
        /// <param name="userRpSubscriptionId">This is the subscription id that is present for te user RP in the arm manifest</param>
        /// <param name="providerNamespace"></param>
        /// <param name="resourceType"></param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> ListAllResourcesOfTypeAsync<T>(Guid userRpSubscriptionId, string providerNamespace, string resourceType, string apiVersion) where T : ARMResource;

        /// <summary>
        /// Patch operation for given api version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> PatchOperationAsync<T>(T operation, string apiVersion) where T : OperationResource;

        /// <summary>
        /// Patch operation status for given api version
        /// </summary>
        /// <param name="operationStatusId"></param>
        /// <param name="state"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> PatchOperationStatusAsync(
            string operationStatusId,
            ProvisioningState state,
            string errorCode,
            string errorMessage,
            string apiVersion);
    }
}
