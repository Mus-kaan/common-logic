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
        public const string GetRelationshipsByMonitor = nameof(GetRelationshipsByMonitor);
        public const string GetRelationshipsByResource = nameof(GetRelationshipsByResource);
        public const string RemoveRelationshipByResource = nameof(RemoveRelationshipByResource);
        #endregion
    }
}
