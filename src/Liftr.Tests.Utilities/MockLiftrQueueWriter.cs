//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Queue;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Tests.Utilities
{
    public class MockLiftrQueueWriter : IQueueWriter
    {
        private object _syncObject = new object();

        public MockLiftrQueueWriter()
        {
            Messages = new List<string>();
        }

        public List<string> Messages { get; }

        public async Task AddMessageAsync(string message, TimeSpan? messageVisibilityTimeout = null, CancellationToken cancellationToken = default)
        {
            lock (_syncObject)
            {
                Messages.Add(message);
            }

            await Task.Delay(3);
        }
    }
}
