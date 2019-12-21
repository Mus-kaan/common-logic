//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class HostingOptions
    {
        public string PartnerName { get; set; }

        public string ShortPartnerName { get; set; }

        public string SecretPrefix { get; set; }

        public int StorageCountPerDataPlaneSubscription { get; set; }

        public IEnumerable<HostingEnvironmentOptions> Environments { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(PartnerName))
            {
                throw new InvalidOperationException($"{nameof(PartnerName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(ShortPartnerName))
            {
                throw new InvalidOperationException($"{nameof(ShortPartnerName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(SecretPrefix))
            {
                throw new InvalidOperationException($"{nameof(SecretPrefix)} cannot be null or empty.");
            }

            if (StorageCountPerDataPlaneSubscription < 0)
            {
                throw new InvalidOperationException($"{nameof(StorageCountPerDataPlaneSubscription)} cannot be negative.");
            }

            if (Environments == null || !Environments.Any())
            {
                throw new InvalidOperationException($"Please specify at leat one environment in the '{nameof(Environments)}' section.");
            }

            foreach (var env in Environments)
            {
                env.CheckValid();
            }
        }
    }
}
