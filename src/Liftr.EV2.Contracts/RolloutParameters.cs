//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// The object representation of parameters for the Rollout system.
    /// </summary>
    public class RolloutParameters : Document
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RolloutParameters"/> class.
        /// </summary>
        public RolloutParameters()
        {
            Schema = new Uri("http://schema.express.azure.com/schemas/2015-01-01-alpha/RolloutParameters.json");
            ContentVersion = "1.0.0.0";
        }

        /// <summary>
        /// Gets or sets the Shell parameters.
        /// </summary>
        public IEnumerable<Shell> ShellExtensions { get; set; }
    }
}
