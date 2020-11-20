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

        /// <summary>
        /// When the message was created.
        /// </summary>
        public string CreatedAt { get; set; }

        /// <summary>
        /// The time the Message was inserted into the Queue.
        /// </summary>
        public DateTimeOffset? InsertedOn { get; set; }

        /// <summary>
        /// The time that the Message will expire and be automatically deleted.
        /// </summary>
        public DateTimeOffset? ExpiresOn { get; set; }

        /// <summary>
        /// The number of times the message has been dequeued.
        /// </summary>
        public long DequeueCount { get; set; }

        public string Version { get; set; }
    }

    public static class QueueMessageVersion
    {
        public const string v2 = nameof(v2);
    }
}
