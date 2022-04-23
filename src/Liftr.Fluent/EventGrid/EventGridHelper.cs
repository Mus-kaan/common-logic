//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.EventGrid;
using Microsoft.Azure.Management.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal class EventGridHelper
    {
        private const string c_eventSubscriptionTemplateFile = "Microsoft.Liftr.Fluent.EventGrid.EventSubscriptionTemplate.json";
        private const string c_asmScanEndpoint = "https://asmimgscanprdwestus.azurewebsites.net/api/ScanOnPushWebhook";
        private const string c_acrEventSubscriptionName = "scanonpush";

        private readonly Serilog.ILogger _logger;

        public EventGridHelper(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public async Task<bool> IsEventSubscriptionExistingAsync(ILiftrAzure liftrAzure, string scope, string eventSubscriptionName = c_acrEventSubscriptionName)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            EventGridManagementClient eventGridManagementClient = new EventGridManagementClient(liftrAzure.AzureCredentials);
            Rest.Azure.AzureOperationResponse<EventSubscription> result;
            try
            {
                result = await eventGridManagementClient.EventSubscriptions.GetWithHttpMessagesAsync(scope, eventSubscriptionName);
            }
            catch (Rest.Azure.CloudException ex)
            {
                _logger.Information("Event subscription: {eventSubscriptionName} doesn't exist: {ex}", eventSubscriptionName, ex);
                return false;
            }
            finally
            {
                eventGridManagementClient.Dispose();
            }

            return result.Response.StatusCode.Equals(HttpStatusCode.OK);
        }

        public async Task CreateImageScanningEventSubscriptionForACRAsync(
                    ILiftrAzure liftrAzure,
                    IRegistry acr,
                    string eventSubscriptionName = c_acrEventSubscriptionName)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (acr == null)
            {
                throw new ArgumentNullException(nameof(acr));
            }

            _logger.Information("Creating an Event Subscription with name {eventSubscriptionName} ...", eventSubscriptionName);
            var templateContent = EmbeddedContentReader.GetContent(c_eventSubscriptionTemplateFile);
            dynamic configObj = JObject.Parse(templateContent);
            var r = configObj.resources[0];
            r.scope = "Microsoft.ContainerRegistry/registries/" + acr.Name;
            r.properties.topic = acr.Id;
            r.properties.destination.properties.endpointUrl = c_asmScanEndpoint;
            templateContent = JsonConvert.SerializeObject(configObj, Formatting.Indented);

            await liftrAzure.CreateDeploymentAsync(acr.Region, acr.ResourceGroupName, templateContent, noLogging: true);
        }
    }
}
