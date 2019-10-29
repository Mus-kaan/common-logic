//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog.Context;
using System;

namespace Microsoft.Liftr.Logging
{
    public sealed class LogContextPropertyScope : IDisposable
    {
        private readonly IDisposable _logContext;

        public LogContextPropertyScope(string name, string value)
        {
            _logContext = string.IsNullOrEmpty(value) ? null : LogContext.PushProperty(name, value);
        }

        public void Dispose()
        {
            _logContext?.Dispose();
        }
    }
}
