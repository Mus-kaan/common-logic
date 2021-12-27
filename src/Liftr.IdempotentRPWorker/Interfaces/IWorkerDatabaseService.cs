//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.ARM;
using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.IdempotentRPWorker.Interfaces
{
    public interface IWorkerDatabaseService
    {
        #region Resource operations

        /// <summary>
        /// Retrieves resource for given resource id and api version
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="tenantId">Tenant in which the resource exists</param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<T> GetResourceAsync<T>(string resourceId, string tenantId, string apiVersion);

        /// <summary>
        /// Deletes resource for given resource id and api version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resourceId"></param>
        /// <param name="tenantId"></param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> DeleteResourceAsync<T>(string resourceId, string tenantId, string apiVersion);

        /// <summary>
        /// Updates resource for given api version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resource"></param>
        /// <param name="stateContext"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> PutResourceAsync<T>(T resource, StateContext stateContext);

        /// <summary>
        /// Updates resource for given api version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resource"></param>
        /// <param name="stateContext"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> PatchResourceAsync<T>(T resource, StateContext stateContext);

        /// <summary>
        /// Gets a list of resources.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resourcePath"></param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> ListResourcesAsync<T>(string resourcePath, string apiVersion);

        /// <summary>
        /// Gets a list of resources filtered by condition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resourcePath"></param>
        /// <param name="apiVersion"></param>
        /// <param name="filterCondition"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> ListFilteredResourcesAsync<T>(string resourcePath, string apiVersion, string filterCondition);

        #endregion

        #region Operation operations

        /// <summary>
        /// Patch operation for given api version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="tenantId">Tenant in which the resource exists</param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> PatchOperationAsync<T>(T operation, string tenantId, string apiVersion) where T : OperationResource;

        /// <summary>
        /// Patch operation status for given api version
        /// </summary>
        /// <param name="operationStatusId"></param>
        /// <param name="state"></param>
        /// <param name="tenantId">Tenant in which the resource exists</param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> PatchOperationStatusAsync(
            string operationStatusId,
            ProvisioningState state,
            string tenantId,
            string errorCode,
            string errorMessage,
            string apiVersion);

        #endregion
    }
}
