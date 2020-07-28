//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
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
            string kubernetesVersion,
            string rootUserName,
            string sshPublicKey,
            string vmSizeType,
            int vmCount,
            string agentPoolProfileName,
            IDictionary<string, string> tags,
            ISubnet subnet = null)
        {
            // https://docs.microsoft.com/en-us/azure/templates/microsoft.containerservice/2020-04-01/managedclusters#ManagedClusterIdentity
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (tags == null)
            {
                tags = new Dictionary<string, string>();
            }

            var templateContent = EmbeddedContentReader.GetContent(c_aksTemplateFile);
            templateContent = templateContent.Replace(c_aksNamePlaceHolder, aksName);

            dynamic configObj = JObject.Parse(templateContent);
            var r = configObj.resources[0];
            r.location = location.ToString();
            r.tags = tags.ToJObject();

            var props = r.properties;
            props.kubernetesVersion = kubernetesVersion;
            props.dnsPrefix = aksName;

            var ap = props.agentPoolProfiles[0];
            ap.name = agentPoolProfileName;
            ap.vmSize = vmSizeType;
            ap.count = vmCount;
            if (subnet != null)
            {
                ap.vnetSubnetID = subnet.Inner.Id;
            }

            props.linuxProfile.adminUsername = rootUserName;
            props.linuxProfile.ssh.publicKeys[0].keyData = sshPublicKey;

            return JsonConvert.SerializeObject(configObj, Formatting.Indented);
        }
    }
}
