//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    /// <summary>
    /// Error from a REST request.
    /// </summary>
    public sealed class ResourceProviderDefaultErrorResponse
    {
        public ErrorResponseBody Error { get; set; }
    }

    public sealed class ErrorResponseBody
    {
        public int Code { get; set; }

        public string Message { get; set; }

        public string Target { get; set; }

        public IEnumerable<ErrorResponseBody> Details { get; set; }
    }
}
