//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     A class that contains information on the schema and version of a document.
    /// </summary>
    public abstract class Document
    {
        /// <summary>
        ///     The Uri pointing to the Json schema that this document conforms to.
        ///     Add JsonProperty "$schema" which is used in spec and service model files,
        ///     so it can be properly deserialized or serialized
        /// </summary>
        [JsonProperty("$schema")]
        public Uri Schema { get; set; }

        /// <summary>
        ///     The version of the schema that this document conforms to.
        /// </summary>
        public string ContentVersion { get; set; }
    }
}
