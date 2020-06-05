//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Net;

namespace Microsoft.Liftr.Utilities
{
    public static class HttpStatusCodeExtensions
    {
        /// <summary>
        /// A value that indicates if the HTTP response was successful. true if System.Net.Http.HttpResponseMessage.StatusCode
        /// was in the range 200-299; otherwise false.
        /// </summary>
        public static bool IsSuccess(this HttpStatusCode statusCode)
        {
            int val = (int)statusCode;
            if (val >= 200 && val <= 299)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
