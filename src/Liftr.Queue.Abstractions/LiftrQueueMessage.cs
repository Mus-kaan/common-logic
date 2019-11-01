//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DiagnosticSource;
using System;

namespace Microsoft.Liftr.Queue
{
    public sealed class LiftrQueueMessage
    {
        public string MsgId { get; set; }

        public string Content { get; set; }

        public TelemetryContext MsgTelemetryContext { get; set; }

        public string CreatedAt { get; set; }
    }
}
