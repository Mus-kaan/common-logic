//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Microsoft.Liftr.Marketplace.ARM.Contracts
{
    public class MigrationResponse
    {
        /// <summary>
        /// Http StatusCode
        /// </summary>
        [JsonProperty("StatusCode")]
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Reason of failure from server if any
        /// </summary>
        [JsonProperty("ReasonPhrase")]
        public string ReasonPhrase { get; set; }

        /// <summary>
        /// Is the Migration response successful
        /// </summary>
        [JsonProperty("IsSuccessful")]
        public bool IsSuccessful { get; set; }

        public static MigrationResponse BuildMigrationResponseSuccess()
        {
            return new MigrationResponse()
            {
                ReasonPhrase = "Successfully migrated the saas resource",
                StatusCode = HttpStatusCode.OK,
                IsSuccessful = true,
            };
        }

        public static MigrationResponse BuildMigrationResponseFailed(HttpStatusCode statusCode, string msg)
        {
            return new MigrationResponse()
            {
                ReasonPhrase = msg,
                StatusCode = statusCode,
                IsSuccessful = false,
            };
        }
    }
}
