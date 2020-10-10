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

        public const string EventhubDisableIngest = nameof(EventhubDisableIngest);
        public const string EventhubEnableIngest = nameof(EventhubEnableIngest);

        public const string EventhubDisableConsuming = nameof(EventhubDisableConsuming);
        public const string EventhubEnableConsuming = nameof(EventhubEnableConsuming);
        #endregion

        #region Datadog
        public const string GetDatadogMonitor = nameof(GetDatadogMonitor);
        public const string ListDatadogMonitor = nameof(ListDatadogMonitor);
        #endregion
    }
}
