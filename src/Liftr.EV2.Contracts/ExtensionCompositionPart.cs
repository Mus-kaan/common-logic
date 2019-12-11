//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// Extension composition part class
    /// </summary>
    public class ExtensionCompositionPart
    {
        /// <summary>
        /// Gets or sets the ARM rollout parameter file path
        /// </summary>
        public string RolloutParametersPath { get; set; }

        /// <summary>
        /// Gets allowed extension types
        /// </summary>
        public HashSet<ExtensionItem> AllowedTypes { get; } = new HashSet<ExtensionItem>();

        /// <summary>
        /// Gets or sets the Http extension definitions.
        /// </summary>
        public IEnumerable<ExtensionItem> Http { get; set; }

        /// <summary>
        /// Gets or sets the shell extension definitions.
        /// </summary>
        public IEnumerable<ExtensionItem> Shell { get; set; }
    }
}
