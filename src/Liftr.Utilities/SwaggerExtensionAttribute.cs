//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Utilities
{
    /// <summary>
    /// Possible values for the x-ms-mutability extension. More information at
    /// https://github.com/Azure/autorest/tree/master/docs/extensions#x-ms-mutability.
    /// </summary>
    [Flags]
    public enum MutabilityValues
    {
        None = 0,
        Create = 1,
        Read = 2,
        Update = 4,
    }

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

        /// <summary>
        /// Values for the x-ms-mutability extension. Defaulted to None.
        /// </summary>
        public MutabilityValues MutabilityValues { get; set; } = MutabilityValues.None;
    }
}
