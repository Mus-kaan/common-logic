//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Utilities;
using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts.ARM
{
    /// <summary>
    /// An Azure resource.
    /// </summary>
    public abstract class ARMResource : IARMResource
    {
        /// <summary>
        /// The ARM id of the resource.
        /// </summary>
        [SwaggerExtension(MarkAsReadOnly = true)]
        public virtual string Id { get; set; }

        /// <summary>
        /// The name of the resource.
        /// </summary>
        [SwaggerExtension(MarkAsReadOnly = true)]
        public virtual string Name { get; set; }

        /// <summary>
        /// The type of the resource.
        /// </summary>
        public abstract string Type { get; set; }

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
