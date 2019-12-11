//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// The secret volume.
    /// </summary>
    public class ShellSecretVolume : ShellVolume
    {
        /// <summary>
        /// Gets or sets the secrets.
        /// </summary>
        public IEnumerable<ShellSecret> Secrets { get; set; }
    }
}
