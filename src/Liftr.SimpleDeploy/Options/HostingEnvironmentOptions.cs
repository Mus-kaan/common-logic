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
    public class HostingEnvironmentOptions
    {
        public EnvironmentType EnvironmentName { get; set; }

        public Guid TenantId { get; set; }

        public Guid AzureSubscription { get; set; }

        public GlobalOptions Global { get; set; }

        public IEnumerable<RegionOptions> Regions { get; set; }

        public string GenevaCertificateSubjectName { get; set; }

        public string FirstPartyAppCertificateSubjectName { get; set; }

        public AKSInfo AKSConfigurations { get; set; } = new AKSInfo();

        public bool EnableVNet { get; set; } = true;

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(GenevaCertificateSubjectName))
            {
                throw new InvalidOperationException($"{nameof(GenevaCertificateSubjectName)} cannot be null or empty.");
            }

            if (Global == null)
            {
                throw new InvalidOperationException($"{nameof(Global)} cannot be null.");
            }

            Global.CheckValid();

            if (Regions == null || !Regions.Any())
            {
                throw new InvalidOperationException($"Please specify at leat one region in the '{nameof(Regions)}' section.");
            }

            foreach (var region in Regions)
            {
                region.CheckValid();
            }

            if (AKSConfigurations == null)
            {
                throw new InvalidOperationException($"{nameof(AKSConfigurations)} cannot be null.");
            }
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
                throw new InvalidOperationException($"{nameof(BaseName)} cannot be null or empty.");
            }
        }
    }

    public class RegionOptions
    {
        [JsonConverter(typeof(RegionConverter))]
        public Region Location { get; set; }

        public string DataBaseName { get; set; }

        public string ComputeBaseName { get; set; }

        public string HostName { get; set; }

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(DataBaseName))
            {
                throw new InvalidOperationException($"{nameof(DataBaseName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(ComputeBaseName))
            {
                throw new InvalidOperationException($"{nameof(ComputeBaseName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(HostName))
            {
                throw new InvalidOperationException($"{nameof(HostName)} cannot be null or empty.");
            }
        }
    }
}
