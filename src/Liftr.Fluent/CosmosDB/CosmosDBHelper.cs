//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal class CosmosDBHelper
    {
        private const string c_cosmosDBTemplateFile = "Microsoft.Liftr.Fluent.CosmosDB.CosmosDBTemplate.json";
        private readonly Serilog.ILogger _logger;

        public CosmosDBHelper(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ICosmosDBAccount> CreateCosmosDBAsync(
            ILiftrAzure liftrAzure,
            Region location,
            string rgName,
            string cosmosDBName,
            IDictionary<string, string> tags,
            bool? isZoneRedundant = null,
            CancellationToken cancellationToken = default)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            isZoneRedundant = (isZoneRedundant ?? true) && AvailabilityZoneRegionLookup.HasSupportCosmosDB(location);
            _logger.Information($"Creating a CosmosDB with name {cosmosDBName} with zone redudandancy set to {isZoneRedundant}...");

            // https://docs.microsoft.com/en-us/azure/templates/microsoft.documentdb/2021-04-15/databaseaccounts
            var templateContent = EmbeddedContentReader.GetContent(c_cosmosDBTemplateFile);

            dynamic configObj = JObject.Parse(templateContent);
            var r = configObj.resources[0];
            r.name = cosmosDBName;
            r.location = location.ToString();
            r.tags = tags.ToJObject();
            r.properties.locations[0].locationName = location.ToString();
            r.properties.locations[0].isZoneRedundant = isZoneRedundant;
            templateContent = JsonConvert.SerializeObject(configObj, Formatting.Indented);
            await liftrAzure.CreateDeploymentAsync(location, rgName, templateContent, noLogging: true, cancellationToken: cancellationToken);

            return await liftrAzure.GetCosmosDBAsync(rgName, cosmosDBName, cancellationToken);
        }
    }
}
