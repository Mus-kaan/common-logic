//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Hosting.Swagger
{
    /// <summary>
    /// Error from a REST request.
    /// </summary>
    public sealed class ResourceProviderDefaultErrorResponse
    {
        /// <summary>
        /// The error object.
        /// </summary>
        public ErrorResponseBody Error { get; set; }
    }

    /// <summary>
    /// The definition of an error object.
    /// </summary>
    public sealed class ErrorResponseBody
    {
        /// <summary>
        /// The error code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The error target.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Additional details and inner errors.
        /// </summary>
        public IEnumerable<ErrorResponseBody> Details { get; set; }
    }
}
