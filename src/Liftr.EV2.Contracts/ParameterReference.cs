//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// The parameter reference class
    /// </summary>
    public class ParameterReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterReference"/> class.
        /// </summary>
        public ParameterReference()
        {
        }

        /// <summary>
        /// The artifact path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Enables scope tag bindings.
        /// </summary>
        public string EnableScopeTagBindings { get; set; }

        /// <summary>
        /// The reference provider.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// The provider parameters.
        /// </summary>
        public IDictionary<string, string> Parameters { get; set; }
    }
}
