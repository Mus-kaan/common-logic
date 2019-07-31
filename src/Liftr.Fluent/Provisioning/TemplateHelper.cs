﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public static class TemplateHelper
    {
        private const string c_plsVNETTemplatePath = "Provisioning\\MultiTenant\\plsVNetTemplate.json";
        private const string c_privateLinkServiceTemplatePath = "Provisioning\\MultiTenant\\privateLinkServiceTemplate.json";

        public static string GeneratePLSVNetTemplate(
            Region location,
            IDictionary<string, string> tags,
            string vnetName,
            string vnetCIDR,
            string plsSubnet,
            string plsSubnetCIDR,
            string publicFrontSubnet,
            string publicFrontSubnetCIDR,
            string backendSubnet,
            string backendSubnetCIDR)
        {
            if (!File.Exists(c_plsVNETTemplatePath))
            {
                throw new InvalidOperationException($"Cannot find the template file at path: {c_plsVNETTemplatePath}");
            }

            var templateContent = File.ReadAllText(c_plsVNETTemplatePath);
            dynamic obj = JObject.Parse(templateContent);
            var r = obj.resources[0];
            r.name = vnetName;
            r.location = location.ToString();
            r.tags = tags.ToJObject();
            r.properties.addressSpace.addressPrefixes[0] = vnetCIDR;
            {
                var subnet = r.properties.subnets[0];
                subnet.name = plsSubnet;
                subnet.properties.addressPrefix = plsSubnetCIDR;
            }

            {
                var subnet = r.properties.subnets[1];
                subnet.name = publicFrontSubnet;
                subnet.properties.addressPrefix = publicFrontSubnetCIDR;
            }

            {
                var subnet = r.properties.subnets[2];
                subnet.name = backendSubnet;
                subnet.properties.addressPrefix = backendSubnetCIDR;
            }

            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public static string GeneratePrivateLinkServiceTemplate(Region location, string privateLinkServiceName, string ilbSubnetId, string ilbFrontendId)
        {
            if (!File.Exists(c_privateLinkServiceTemplatePath))
            {
                throw new InvalidOperationException($"Cannot find the template file at path: {c_privateLinkServiceTemplatePath}");
            }

            var templateContent = File.ReadAllText(c_privateLinkServiceTemplatePath);
            dynamic obj = JObject.Parse(templateContent);
            var r = obj.resources[0];
            r.name = privateLinkServiceName;
            r.location = location.ToString();
            r.properties.ipConfigurations[0].properties.subnet.id = ilbSubnetId;
            r.properties.loadBalancerFrontendIpConfigurations[0].id = ilbFrontendId;
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
