//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;

namespace Microsoft.Liftr.RPaaS
{
    [Serializable]
    public class MetaRPException : Exception
    {
        public MetaRPException()
        {
        }

        public MetaRPException(string message)
            : base(message)
        {
        }

        public MetaRPException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MetaRPException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            StatusCode = (HttpStatusCode)info?.GetInt32(nameof(StatusCode));
            ErrorResponse = (ErrorResponse)info?.GetValue(nameof(ErrorResponse), typeof(ErrorResponse));
        }

        public HttpStatusCode? StatusCode { get; set; }

        /// <summary>
        /// The complete description of the error.
        /// </summary>
        public ErrorResponse ErrorResponse { get; set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info?.AddValue(nameof(StatusCode), StatusCode);
            info?.AddValue(nameof(ErrorResponse), ErrorResponse);
            base.GetObjectData(info, context);
        }

        public static MetaRPException Create(HttpResponseMessage response, string target)
        {
            return new MetaRPException(response?.ToString())
            {
                StatusCode = response.StatusCode,
                ErrorResponse = ErrorResponse.Create(
                               code: response.StatusCode.ToString(),
                               message: response.ReasonPhrase,
                               target: target),
            };
        }
    }
}
