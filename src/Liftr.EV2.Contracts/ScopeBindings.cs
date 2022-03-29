//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// The Shell.
    /// </summary>
    public class ScopeBindings
    {
        /// <summary>
        /// Gets or sets the scope tag name.
        /// </summary>
        public string ScopeTagName { get; set; }

        /// <summary>
        /// Gets or sets the bindings.
        /// </summary>
        public IEnumerable<Bindings> Bindings { get; set; }
    }

    /// <summary>
    /// The bindings.
    /// </summary>
    public class Bindings
    {
        /// <summary>
        /// Gets or sets the string to be replaced.
        /// </summary>
        public string Find { get; set; }

        /// <summary>
        /// Gets or sets the string to replace with.
        /// </summary>
        public string ReplaceWith { get; set; }
    }
}
