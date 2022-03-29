//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.Hosting.Contracts
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

        /// <summary>
        /// The ACIS extensions that will be supported.
        /// When this is not null, it will create ACIS supporting resources in each region.
        /// See details: https://genevamondocs.azurewebsites.net/actions/How%20Do%20I/keyvault.html
        /// </summary>
        public string AllowedAcisExtensions { get; set; }

        public int StorageCountPerDataPlaneSubscription { get; set; }

        public bool DBSupport { get; set; } = true;

        /// <summary>
        /// See more at: https://thanos.io/
        /// </summary>
        public bool EnableThanos { get; set; } = false;

        /// <summary>
        /// Automation mode for geneva and some other images. Using Liftr Common image versions.
        /// </summary>
        public bool EnableLiftrCommonImages { get; set; } = false;

        /// <summary>
        /// The IcM connector Id. https://aka.ms/prom2icm
        /// </summary>
        public string IcMConnectorId { get; set; }

        /// <summary>
        /// The IcM notification email. https://aka.ms/prom2icm
        /// </summary>
        public string IcMNotificationEmail { get; set; }

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

                if (EnableThanos && string.IsNullOrEmpty(env.ThanosClientIPRange))
                {
                    throw new InvalidHostingOptionException($"Since Thanos is enabled, please provide '{nameof(env.ThanosClientIPRange)}'.");
                }
            }
        }
    }
}
