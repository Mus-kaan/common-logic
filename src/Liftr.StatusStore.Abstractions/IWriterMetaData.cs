//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.StatusStore
{
    public interface IWriterMetaData
    {
        string MachineName { get; set; }

        string RunningSessionId { get; set; }

        DateTime ProcessStartTime { get; set; }

        string SubscriptionId { get; set; }

        string ResourceGroup { get; set; }

        string VMName { get; set; }

        string Region { get; set; }
    }
}
