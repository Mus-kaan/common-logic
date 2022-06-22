//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.ACIS.Relay;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.ARM;
using Microsoft.Liftr.RPaaS;
using Microsoft.Liftr.Utilities;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Notification
{
    public class SendEmail
    {
        private const int resultsLimit = 100;
        private readonly IMetaRPStorageClient _metaRPClient;
        private readonly ITenantHelper _tenantHelper;
        private readonly ILogger _logger;
        private readonly IResourceEntityDataSource _ResourceEntityDataSource;

        public SendEmail(
            IMetaRPStorageClient metaRPClient,
            ITenantHelper tenantHelper,
            IResourceEntityDataSource EntityDataSource,
            ILogger logger)
        {
            _metaRPClient = metaRPClient ?? throw new ArgumentNullException(nameof(metaRPClient));
            _tenantHelper = tenantHelper ?? throw new ArgumentNullException(nameof(tenantHelper));
            _ResourceEntityDataSource = EntityDataSource ?? throw new ArgumentNullException(nameof(EntityDataSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MonitorResource> GetMonitorAsync(string resourceId, IACISOperation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            var rid = new ResourceId(resourceId);
            var tenantId = await _tenantHelper.GetTenantIdForSubscriptionAsync(rid.SubscriptionId);

            await operation.LogInfoAsync("Retrieving metadata from RPaaS ...");
            MonitorResource resource;
            try
            {
                resource = await _metaRPClient.GetResourceAsync<MonitorResource>(
                    resourceId, tenantId, Constants.ApiVersion2020_02_preview);
            }
            catch (MetaRPException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // The NotFound status code means this resource does not exist yet. We set the resource to null.
                    resource = null;
                }
                else
                {
                    // Other statuses cannot be handled here; we just re-throw the excpetion.
                    throw;
                }
            }

            if (resource != null)
            {
                // hide credentials.
                resource.Properties.InternalMetadata = null;
                if (!string.IsNullOrEmpty(resource.Properties?.OrganizationProperties?.LinkingAuthCode))
                {
                    resource.Properties.OrganizationProperties.LinkingAuthCode = "hidden";
                }

                resource.RemoveUserInfo();

                await operation.SuccessfulFinishAsync(resource.ToJson(indented: true));
            }
            else
            {
                await operation.SuccessfulFinishAsync(string.Empty);
            }

            return resource;
        }

        public async Task<List<string>> GetAllMonitorsAsync(IACISOperation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            List<string> responseList = new List<string>();
            try
            {
                await operation.LogInfoAsync("Started fetching the monitors from the collection");
                List<ResourceEntity> ResourceList = await _ResourceEntityDataSource.GetAllMonitorsAsync();
                await operation.LogInfoAsync("Fetched the monitors from the collection");
                ResourceList.ForEach(Resource =>
                {
                    responseList.Add(Resource.ResourceId);
                    _logger.Information("The resource id is {resourceId}", Resource.ResourceId);
                });
                await operation.LogInfoAsync($"The count of the monitors : {responseList.Count}");
                string resourceIdList = string.Join(",", responseList);
                await operation.SuccessfulFinishAsync(resourceIdList);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception occured while executing the operation : {exception} with stackTrace {stackTrace}", ex.Message, ex.StackTrace);
                await operation.LogErrorAsync("Error occured while executing the operation");
                await operation.FailAsync("Failed to execute the operation");
            }

            return responseList;
        }
    }
}
