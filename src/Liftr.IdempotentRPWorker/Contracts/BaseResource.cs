//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.ARM;
using Microsoft.Liftr.Utilities;

namespace Microsoft.Liftr.IdempotentRPWorker
{
    public class BaseResource : ARMResource
    {
        /// <summary>
        /// The ARM id of the resource.
        /// </summary>
        [SwaggerExtension(MarkAsReadOnly = true)]
        public override string Id
        {
            get
            {
                return base.Id;
            }

            set
            {
                base.Id = value;
            }
        }

        /// <summary>
        /// The name of the resource.
        /// </summary>
        [SwaggerExtension(MarkAsReadOnly = true)]
        public override string Name
        {
            get
            {
                return base.Name;
            }

            set
            {
                base.Name = value;
            }
        }

        /// <summary>
        /// The type of the resource.
        /// </summary>
        [SwaggerExtension(MarkAsReadOnly = true)]
        public override string Type { get; set; }
    }
}
