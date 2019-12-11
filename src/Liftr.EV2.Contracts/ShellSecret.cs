//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// The shell secret.
    /// </summary>
    public class ShellSecret
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the reference.
        /// </summary>
        public ParameterReference Reference { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the flag to convert the value to base 64.
        /// </summary>
        public bool ConvertToBase64 { get; set; }
    }
}
