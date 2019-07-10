//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts.ARM
{
    /// <summary>
    /// An Azure resource.
    /// </summary>
    public class ARMResource : IARMResource
    {
        /// <summary>
        /// The resource ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the resource.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The tags of the resource.
        /// </summary>
        public IDictionary<string, string> Tags { get; set; }

        /// <summary>
        /// The location of the resource. This cannot be changed after the resource is created.
        /// </summary>
        public string Location { get; set; }
    }
}
