//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Liftr.DiagnosticSource
{
    public static class CallContextHolder
    {
        /// <summary>
        /// These headers will be automatically injected to all out-going dependency http requests.
        /// </summary>
        public static readonly AsyncLocal<IDictionary<string, string>> CommonHttpHeaders = new AsyncLocal<IDictionary<string, string>>();
    }
}
