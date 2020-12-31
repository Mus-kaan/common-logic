//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.ARM;
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
        #region Resource operations

        /// <summary>
        /// Retrieves ARM resource for given resource id and api version from MetaRP
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="tenantId">Tenant in which the resource exists</param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<T> GetResourceAsync<T>(string resourceId, string tenantId, string apiVersion);

        /// <summary>
        /// Updates ARM resource for given api version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resource"></param>
        /// <param name="resourceId"></param>
        /// <param name="tenantId">Tenant in which the resource exists</param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> PutResourceAsync<T>(T resource, string resourceId, string tenantId, string apiVersion);

        /// <summary>
        /// Updates ARM resource for given api version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resource"></param>
        /// <param name="resourceId"></param>
        /// <param name="tenantId">Tenant in which the resource exists</param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> PatchResourceAsync<T>(T resource, string resourceId, string tenantId, string apiVersion);

        /// <summary>
        /// Gets a list of resources.
        /// To get top-level resource, resourcePath should be /{userRpSubscriptionId}/providers/{providerNamespace}/{resourceType}.
        /// To get sub-resources, resourcePath should be /{resourceId}/{subResourcesType}.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resourcePath"></param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> ListResourcesAsync<T>(string resourcePath, string apiVersion);

        /// <summary>
        /// Gets a list of resources filtered by condition
        /// To get top-level resource, resourcePath should be /{userRpSubscriptionId}/providers/{providerNamespace}/{resourceType}.
        /// To get sub-resources, resourcePath should be /{resourceId}/{subResourcesType}.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resourcePath"></param>
        /// <param name="apiVersion"></param>
        /// <param name="filterCondition"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> ListFilteredResourcesAsync<T>(string resourcePath, string apiVersion, string filterCondition);

        #endregion

        #region Subscription operations

        /// <summary>
        /// Returns the tenant associated to a subscription. In case more details are
        /// needed, use GetResourceAsync with RegisteredSubscriptionModel class.
        /// </summary>
        /// <param name="userRpSubscriptionId"></param>
        /// <param name="providerName"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<string> GetTenantForSubscriptionAsync(
            string userRpSubscriptionId, string providerName, string subscriptionId, string apiVersion);

        /// <summary>
        /// Returns mapping of subscriptions to their tenants. In case more details are
        /// needed, use ListResourcesAsync with RegisteredSubscriptionModel class.
        /// </summary>
        /// <param name="userRpSubscriptionId"></param>
        /// <param name="providerName"></param>
        /// <param name="apiVersion"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetTenantForAllSubscriptionsAsync(
            string userRpSubscriptionId, string providerName, string apiVersion);

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
