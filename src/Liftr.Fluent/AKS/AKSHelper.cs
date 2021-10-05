//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Fluent
{
    internal static class AKSHelper
    {
        private const string c_aksNamePlaceHolder = "PLACEHOLDER_AKS_NAME";
        private const string c_aksTemplateFile = "Microsoft.Liftr.Fluent.AKS.AKSTemplate.json";

        public static string GenerateAKSTemplate(
            Region location,
            string aksName,
            string rootUserName,
            string sshPublicKey,
            AKSInfo aksInfo,
            string agentPoolProfileName,
            IDictionary<string, string> tags,
            bool supportAvailabilityZone,
            string pIpId,
            ISubnet subnet = null)
        {
            if (aksInfo == null)
            {
                throw new ArgumentNullException(nameof(aksInfo));
            }

            aksInfo.CheckValues();

            // https://docs.microsoft.com/en-us/azure/templates/microsoft.containerservice/2020-04-01/managedclusters#ManagedClusterIdentity
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (tags == null)
            {
                tags = new Dictionary<string, string>();
            }

            string templateContent = EmbeddedContentReader.GetContent(c_aksTemplateFile);

            templateContent = templateContent.Replace(c_aksNamePlaceHolder, aksName);

            dynamic configObj = JObject.Parse(templateContent);
            var r = configObj.resources[0];
            r.location = location.ToString();
            r.tags = tags.ToJObject();

            var props = r.properties;
            props.kubernetesVersion = aksInfo.KubernetesVersion;
            props.dnsPrefix = aksName;

            var ap = props.agentPoolProfiles[0];
            ap.name = agentPoolProfileName;
            ap.vmSize = aksInfo.AKSMachineType.Value;

            if (aksInfo.AKSMachineCount.HasValue)
            {
                ap.count = aksInfo.AKSMachineCount.Value;
            }
            else
            {
                // azure portal AKS template has a default 3 count when auto-scale is enabled.
                ap.count = 3;

                ap.enableAutoScaling = true;
                ap.minCount = aksInfo.AKSAutoScaleMinCount.Value;
                ap.maxCount = aksInfo.AKSAutoScaleMaxCount.Value;
            }

            if (supportAvailabilityZone)
            {
                ap.availabilityZones[0] = "1";
                ap.availabilityZones[1] = "2";
                ap.availabilityZones[2] = "3";
            }
            else
            {
                ap.availabilityZones = null;
            }

            if (subnet != null)
            {
                ap.vnetSubnetID = subnet.Inner.Id;
            }

            props.linuxProfile.adminUsername = rootUserName;
            props.linuxProfile.ssh.publicKeys[0].keyData = sshPublicKey;

            if (pIpId != null)
            {
                props.networkProfile.loadBalancerProfile.outboundIPs.publicIPs[0].id = pIpId;
            }

            return JsonConvert.SerializeObject(configObj, Formatting.Indented);
        }
    }
}
