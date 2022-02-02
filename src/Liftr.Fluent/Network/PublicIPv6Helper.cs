//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal class PublicIPv6Helper
    {
        private const string c_templateFile = "Microsoft.Liftr.Fluent.Network.PublicIPv6Template.json";

        private readonly Serilog.ILogger _logger;

        public PublicIPv6Helper(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public async Task CreatePublicIPv6Async(
            ILiftrAzure liftrAzure,
            Region location,
            string rgName,
            string name,
            IDictionary<string, string> tags)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            var templateContent = GenerateARMTemplate(
                location,
                name,
                tags);

            await liftrAzure.CreateDeploymentAsync(location, rgName, templateContent, noLogging: true);
        }

        private static string GenerateARMTemplate(
            Region location,
            string name,
            IDictionary<string, string> tags)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (tags == null)
            {
                tags = new Dictionary<string, string>();
            }

            var templateContent = EmbeddedContentReader.GetContent(c_templateFile);

            dynamic configObj = JObject.Parse(templateContent);
            var r = configObj.resources[0];
            r.name = name;
            r.location = location.ToString();
            r.tags = tags.ToJObject();

            var dnsSettings = r.properties.dnsSettings;
            dnsSettings.domainNameLabel = name;

            return JsonConvert.SerializeObject(configObj, Formatting.Indented);
        }
    }
}
