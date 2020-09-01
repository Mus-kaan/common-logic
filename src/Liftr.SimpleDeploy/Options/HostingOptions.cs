//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Provisioning;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class HostingOptions
    {
        /// <summary>
        /// Name of the partner, e.g. 'Datadog', 'Gateway'
        /// </summary>
        public string PartnerName { get; set; }

        /// <summary>
        /// Partner short name, it is recommended to keep this short and in lower case. It will be used in almost all the resource names. e.g. 'dg' for 'Datadog'.
        /// </summary>
        public string ShortPartnerName { get; set; }

        public string SecretPrefix { get; set; }

        public string HelmReleaseName { get; set; }

        public int StorageCountPerDataPlaneSubscription { get; set; }

        public bool DBSupport { get; set; } = true;

        /// <summary>
        /// See more at: https://thanos.io/
        /// </summary>
        public bool EnableThanos { get; set; } = false;

        public IEnumerable<HostingEnvironmentOptions> Environments { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(PartnerName))
            {
                throw new InvalidHostingOptionException($"{nameof(PartnerName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(ShortPartnerName))
            {
                throw new InvalidHostingOptionException($"{nameof(ShortPartnerName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(SecretPrefix))
            {
                throw new InvalidHostingOptionException($"{nameof(SecretPrefix)} cannot be null or empty.");
            }

            if (StorageCountPerDataPlaneSubscription < 0)
            {
                throw new InvalidHostingOptionException($"{nameof(StorageCountPerDataPlaneSubscription)} cannot be negative.");
            }

            if (Environments == null || !Environments.Any())
            {
                throw new InvalidHostingOptionException($"Please specify at leat one environment in the '{nameof(Environments)}' section.");
            }

            foreach (var env in Environments)
            {
                env.CheckValid();
            }
        }
    }
}
