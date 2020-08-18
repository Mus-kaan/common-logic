//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Newtonsoft.Json;
using System;
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

    public class HostingEnvironmentOptions
    {
        /// <summary>
        /// Type of the environment. One of [ Production, Canary, DogFood, Dev, Test, Fairfax, Mooncake ]
        /// </summary>
        public EnvironmentType EnvironmentName { get; set; }

        /// <summary>
        /// Azure subscription Id. All the resources will be provisionined in thie subscription.
        /// </summary>
        public Guid AzureSubscription { get; set; }

        /// <summary>
        /// Section for defining the global resources.
        /// </summary>
        public GlobalOptions Global { get; set; }

        /// <summary>
        /// A list of regional definition sections.
        /// </summary>
        public IEnumerable<RegionOptions> Regions { get; set; }

        public Dictionary<string, string> OneCertCertificates { get; set; } = new Dictionary<string, string>();

        public AKSInfo AKSConfigurations { get; set; }

        public VMSSMachineInfo VMSSConfigurations { get; set; }

        public GenevaOptions Geneva { get; set; }

        public int IPPerRegion { get; set; } = 3;

        /// <summary>
        /// Restrict network access only to spcific VNet subnets.
        /// </summary>
        public bool EnableVNet { get; set; } = true;

        public string DomainName { get; set; }

        public string LogAnalyticsWorkspaceId { get; set; }

        public string DiagnosticsStorageId { get; set; }

        public bool IsAKS => AKSConfigurations != null;

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(DomainName))
            {
                throw new InvalidHostingOptionException($"{nameof(DomainName)} cannot be null or empty.");
            }

            if (Global == null)
            {
                throw new InvalidHostingOptionException($"{nameof(Global)} cannot be null.");
            }

            Global.CheckValid();

            if (Regions == null || !Regions.Any())
            {
                throw new InvalidHostingOptionException($"Please specify at leat one region in the '{nameof(Regions)}' section.");
            }

            foreach (var region in Regions)
            {
                region.CheckValid();
            }

            if (AKSConfigurations == null && VMSSConfigurations == null)
            {
                throw new InvalidHostingOptionException($"{nameof(AKSConfigurations)} and {nameof(VMSSConfigurations)} cannot be null at the same time.");
            }

            if (AKSConfigurations != null)
            {
                AKSConfigurations.CheckValues();
            }

            if (VMSSConfigurations != null)
            {
                VMSSConfigurations.CheckValues();
            }

            if (OneCertCertificates == null ||
                !OneCertCertificates.ContainsKey(CertificateName.GenevaClientCert))
            {
                throw new InvalidHostingOptionException($"Please specify the Geneva certificate in the '{nameof(OneCertCertificates)}' section. The certification name must be '{CertificateName.GenevaClientCert}' and please specific a subject name.");
            }

            if (IPPerRegion < 3)
            {
                throw new InvalidHostingOptionException($"{nameof(IPPerRegion)} cannot be less than 3, since there will be no room for swap deployment..");
            }
            else if (IPPerRegion > 100)
            {
                throw new InvalidHostingOptionException($"{nameof(IPPerRegion)} cannot be large than 100.");
            }

            Geneva?.CheckValid();
        }
    }

    public class GlobalOptions
    {
        [JsonConverter(typeof(RegionConverter))]
        public Region Location { get; set; }

        public string BaseName { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(BaseName))
            {
                throw new InvalidHostingOptionException($"{nameof(BaseName)} cannot be null or empty.");
            }
        }
    }

    public class RegionOptions
    {
        [JsonConverter(typeof(RegionConverter))]
        public Region Location { get; set; }

        public string DataBaseName { get; set; }

        public string ComputeBaseName { get; set; }

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(DataBaseName))
            {
                throw new InvalidHostingOptionException($"{nameof(DataBaseName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(ComputeBaseName))
            {
                throw new InvalidHostingOptionException($"{nameof(ComputeBaseName)} cannot be null or empty.");
            }
        }
    }
}
