//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.Whale.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Whale.Interfaces
{
    /// <summary>
    /// Interface for managing monitored resources.
    /// </summary>
    public interface IMonitoredResourceManager
    {
        /// <summary>
        /// Start monitoring a resource, by adding diagnostic settings to it if needed.
        /// </summary>
        Task StartMonitoringResourceAsync(MonitoredResource resource, string monitorId, string partnerEntityId, string tenantId);

        /// <summary>
        /// Stop monitoring a resource, removing the diagnostic setting from it if needed.
        /// </summary>
        Task StopMonitoringResourceAsync(MonitoredResource resource, string monitorId, string partnerEntityId, string tenantId);

        /// <summary>
        /// Start monitoring a subscription, adding a diagnostic setting to it if needed.
        /// </summary>
        Task StartMonitoringSubscriptionAsync(string monitorId, string location, string partnerEntityId, string tenantId);

        /// <summary>
        /// Stop monitoring a subscription, removing the diagnostic setting to it if needed.
        /// </summary>
        Task StopMonitoringSubscriptionAsync(string monitorId, string partnerEntityId, string tenantId);

        /// <summary>
        /// List the resources currently being monitored.
        /// </summary>
        Task<IEnumerable<MonitoredResource>> ListMonitoredResourcesAsync(string partnerEntityId, string tenantId);

        /// <summary>
        /// List the resources tracked for monitoring, regardless of active status of the resource.
        /// </summary>
        Task<IEnumerable<DataSource.Mongo.MonitoringSvc.MonitoringStatus>> ListTrackedResourcesAsync(string partnerEntityId, string tenantId);

        /// <summary>
        /// Stop tracking a non-monitored resource, by removing it from the database.
        /// </summary>
        Task StopTrackingResourceAsync(string resourceId, string monitorId, string partnerEntityId, string tenantId);
    }
}
