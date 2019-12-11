//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// The shell environment variable.
    /// </summary>
    public class ShellEnvironmentVariable
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the reference.
        /// </summary>
        public ParameterReference Reference { get; set; }

        /// <summary>
        /// Gets or sets the secret value.
        /// </summary>
        public bool? AsSecureValue { get; set; }
    }
}
