//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Queue
{
    public sealed class QueueMessageProcessingResult
    {
        public bool SuccessfullyProcessed { get; set; } = true;

        public string ProcessingError { get; set; }
    }
}
