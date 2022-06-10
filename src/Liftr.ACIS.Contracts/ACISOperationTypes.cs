//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ACIS.Contracts
{
    public static class ACISOperationTypes
    {
        #region LogForwarder
        public const string ListEventhub = nameof(ListEventhub);

        public const string AddEventhub = nameof(AddEventhub);
        public const string DeleteEventhub = nameof(DeleteEventhub);

        public const string UpdateEventhub = nameof(UpdateEventhub);

        public const string ListMonitoringRelationship = nameof(ListMonitoringRelationship);
        public const string AddMonitoringRelationship = nameof(AddMonitoringRelationship);
        public const string RemoveMonitoringRelationship = nameof(RemoveMonitoringRelationship);
        #endregion

        #region Datadog
        public const string GetDatadogMonitor = nameof(GetDatadogMonitor);
        public const string ListDatadogMonitor = nameof(ListDatadogMonitor);
        public const string GetMonitorsBySubscription = nameof(GetMonitorsBySubscription);
        public const string ListAllMonitors = nameof(ListAllMonitors);
        public const string GetRelationshipsByMonitor = nameof(GetRelationshipsByMonitor);
        public const string GetRelationshipsByResource = nameof(GetRelationshipsByResource);
        public const string RemoveRelationshipByResource = nameof(RemoveRelationshipByResource);
        public const string GetMonitorTagRules = nameof(GetMonitorTagRules);

        public const string GetMarketplaceResourceByMonitor = nameof(GetMarketplaceResourceByMonitor);
        #endregion

        #region Nginx
        public const string ListAllDeploymentsBySubscriptionId = nameof(ListAllDeploymentsBySubscriptionId);
        public const string GetMarketplaceResourceByDeployment = nameof(GetMarketplaceResourceByDeployment);
        public const string GetNginxDeployment = nameof(GetNginxDeployment);
        public const string GetNginxPartnerDeployment = nameof(GetNginxPartnerDeployment);
        public const string ListPartnerDeployments = nameof(ListPartnerDeployments);
        public const string GetPartnerOrgInfo = nameof(GetPartnerOrgInfo);
        public const string GetVNetInjectionEntityByDeployment = nameof(GetVNetInjectionEntityByDeployment);
        public const string GetHOBOBillingEntity = nameof(GetHOBOBillingEntity);
        public const string GetNginxConfig = nameof(GetNginxConfig);
        public const string CleanupBrokenRPaaSEntityAsync = nameof(CleanupBrokenRPaaSEntityAsync);
        #endregion
    }
}
