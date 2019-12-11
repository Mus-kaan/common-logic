//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// The Shell Launch.
    /// </summary>
    public class ShellLaunch
    {
        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        public IEnumerable<string> Command { get; set; }

        /// <summary>
        /// Gets or sets the environment variables.
        /// </summary>
        public IEnumerable<ShellEnvironmentVariable> EnvironmentVariables { get; set; }

        /// <summary>
        /// Gets or sets the secret volumes.
        /// </summary>
        public IEnumerable<ShellSecretVolume> SecretVolumes { get; set; }

        /// <summary>
        /// Gets or sets the network profile.
        /// </summary>
        public ShellNetworkProfile NetworkProfile { get; set; }

        /// <summary>
        /// Gets or sets the identity.
        /// </summary>
        public ShellIdentity Identity { get; set; }
    }
}