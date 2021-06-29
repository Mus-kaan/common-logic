//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal class ACRHelper
    {
        private const string c_acrTemplateFile = "Microsoft.Liftr.Fluent.ACR.ACRTemplate.json";
        private readonly Serilog.ILogger _logger;

        public ACRHelper(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IRegistry> CreateACRAsync(
            ILiftrAzure liftrAzure,
            Region location,
            string rgName,
            string acrName,
            IDictionary<string, string> tags)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            _logger.Information("Creating an ACR with name {acrName} ...", acrName);
            var templateContent = EmbeddedContentReader.GetContent(c_acrTemplateFile);
            dynamic configObj = JObject.Parse(templateContent);
            var r = configObj.resources[0];
            r.name = acrName;
            r.location = location.ToString();
            r.tags = tags.ToJObject();
            r.properties.zoneRedundancy = AvailabilityZoneRegionLookup.HasSupportACR(location) ? "Enabled" : "Disabled";
            templateContent = JsonConvert.SerializeObject(configObj, Formatting.Indented);
            await liftrAzure.CreateDeploymentAsync(location, rgName, templateContent, noLogging: true);

            return await liftrAzure.GetACRAsync(rgName, acrName);
        }
    }
}
