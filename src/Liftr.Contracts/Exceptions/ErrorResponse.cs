//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;

namespace Microsoft.Liftr.Contracts.Exceptions
{
    /// <summary>
    /// Error response for non successful requests. As per ARM RPC: https://github.com/Azure/azure-resource-manager-rpc/blob/master/v1.0/common-api-details.md
    /// </summary>
    public class ErrorResponse
    {
        private const string DefaultTarget = "Request";

        protected ErrorResponse()
        {
        }

        /// <summary>
        /// The properties that describe the error.
        /// </summary>
        public ErrorDescription Error { get; set; }

        public static ErrorResponse Create(string code, string message, string target, params InnerErrorDescription[] innerErrorDescriptions)
        {
            var error = new ErrorResponse
            {
                Error = new ErrorDescription
                {
                    Code = code,
                    Message = message,
                    Target = target,
                },
            };

            if (innerErrorDescriptions != null && innerErrorDescriptions.Length > 0)
            {
                error.Error.Message = $"{error.Error.Message}. InnerErrors: {string.Join(";", innerErrorDescriptions.Select(e => $"{e.Target ?? DefaultTarget}:{e.Message}"))}";

                error.Error.Details = innerErrorDescriptions;
            }

            return error;
        }

        public static object Create(string v, string message)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// The properties that describe the error.
    /// </summary>
    public class ErrorDescription
    {
        /// <summary>
        /// The error code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The detailed message describing the error.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The target of the particular error.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// An array of JSON objects that MUST contain name/value pairs for code and message, and MAY contain a name/value pair for target, as described above.
        /// </summary>
        public InnerErrorDescription[] Details { get; set; }
    }

    public class InnerErrorDescription
    {
        /// <summary>
        /// The error code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The detailed message describing the error.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The target of the particular error.
        /// </summary>
        public string Target { get; set; }
    }
}
