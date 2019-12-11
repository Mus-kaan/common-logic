//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// The shell identity.
    /// </summary>
    public class ShellIdentity
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the user assigned identities.
        /// </summary>
        public IEnumerable<string> UserAssignedIdentities { get; set; }
    }
}
