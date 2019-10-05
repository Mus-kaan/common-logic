//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Hosting.Swagger
{
    /// <summary>
    /// Custom attribute to modify the generated swagger.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class SwaggerExtensionAttribute : Attribute
    {
        /// <summary>
        /// Property indicating if the schema should be excluded from the swagger.
        /// </summary>
        public bool ExcludeFromSwagger { get; set; } = false;

        /// <summary>
        /// Property indicating if the schema should be marked as readonly in the swagger.
        /// </summary>
        public bool MarkAsReadOnly { get; set; } = false;
    }
}
