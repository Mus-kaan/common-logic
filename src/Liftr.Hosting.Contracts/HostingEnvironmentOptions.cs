//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.Hosting.Contracts
{
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

        public bool EnablePromIcM { get; set; } = true;

        public string DomainName { get; set; }

        public string LogAnalyticsWorkspaceId { get; set; }

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

                if (EnableVNet && region.IsSeparatedDataAndComputeRegion)
                {
                    throw new InvalidHostingOptionException($"Separated data and compute regions does not support VNet.");
                }
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
}
