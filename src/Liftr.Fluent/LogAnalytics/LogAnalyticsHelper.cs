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
    public class LogAnalyticsHelper
    {
        private const string c_templateFile = "Microsoft.Liftr.Fluent.LogAnalytics.LogAnalyticsTemplate.json";
        private readonly Serilog.ILogger _logger;

        public LogAnalyticsHelper(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public async Task CreateLogAnalyticsWorkspaceAsync(
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

            _logger.Information("Creating a Log Analytics Workspace with name '{name}' in resource group '{rgName}' ...", name, rgName);

            var templateContent = GenerateARMTemplate(
                location,
                name,
                tags);

            await liftrAzure.CreateDeploymentAsync(location, rgName, templateContent, noLogging: true);
        }

        public Task<ResourceGetResponse> GetLogAnalyticsWorkspaceAsync(ILiftrAzure liftrAzure, string rgName, string name)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            var resourceId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourcegroups/{rgName}/providers/microsoft.operationalinsights/workspaces/{name}";
            _logger.Information("Getting the Log Analytics Workspace with resource Id: '{resourceId}' ...", resourceId);
            return liftrAzure.GetResourceAsync(resourceId, "2015-03-20");
        }

        private static string GenerateARMTemplate(
            Region location,
            string name,
            IDictionary<string, string> tags)
        {
            // https://docs.microsoft.com/en-us/azure/azure-monitor/learn/quick-create-workspace-cli
            // https://docs.microsoft.com/en-us/azure/templates/Microsoft.OperationalInsights/2015-11-01-preview/workspaces
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

            return JsonConvert.SerializeObject(configObj, Formatting.Indented);
        }
    }
}
