//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;

namespace Microsoft.Liftr.Contracts.Exceptions
{
    [Serializable]
    public class HttpResponseException : Exception
    {
        protected HttpResponseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            StatusCode = (HttpStatusCode)info?.GetInt32(nameof(StatusCode));
            ErrorResponse = (ErrorResponse)info?.GetValue(nameof(ErrorResponse), typeof(ErrorResponse));
        }

        private HttpResponseException()
        {
        }

        private HttpResponseException(string message)
            : base(message)
        {
        }

        private HttpResponseException(string message, Exception inner)
            : base(message, inner)
        {
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

        public static HttpResponseException Create(
            HttpStatusCode statusCode,
            string errorCode,
            string target,
            string message,
            params InnerErrorDescription[] innerErrors) => new HttpResponseException(message)
            {
                StatusCode = statusCode,
                ErrorResponse = ErrorResponse.Create(
                    code: errorCode,
                    message: message,
                    target: target,
                    innerErrorDescriptions: innerErrors),
            };

        public static HttpResponseException Create(
            HttpStatusCode statusCode,
            string errorCode,
            string target,
            string message) => new HttpResponseException(message)
            {
                StatusCode = statusCode,
                ErrorResponse = ErrorResponse.Create(
                    code: errorCode,
                    message: message,
                    target: target),
            };

        public static HttpResponseException Create(
            HttpResponseMessage response, string target) => new HttpResponseException(response?.ToString())
            {
                StatusCode = response.StatusCode,
                ErrorResponse = ErrorResponse.Create(
                    code: response.StatusCode.ToString(),
                    message: response.ReasonPhrase,
                    target: target),
            };
    }
}
