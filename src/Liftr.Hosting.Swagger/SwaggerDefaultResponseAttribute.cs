//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Hosting.Swagger
{
    /// <summary>
    /// Swagger response for Default. ARM (Auto Rest) requires a default response for each API to handle error cases.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SwaggerDefaultResponseAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerDefaultResponseAttribute"/> class.
        /// </summary>
        /// <param name="type">The response type.</param>
        /// <param name="description">Description</param>
        public SwaggerDefaultResponseAttribute(Type type, string description)
        {
            Type = type;
            Description = description;
        }

        public Type Type { get; }

        public string Description { get; }
    }
}
