using Microsoft.AspNetCore.Mvc;
using Microsoft.IFxAudit;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.IFxAuditLinux;
using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Interfaces;
using Microsoft.Liftr.Marketplace.ARM.Models;
using Microsoft.Liftr.MarketplaceResource.DataSource;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Liftr.Sample.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketplaceResourceController : ControllerBase
    {
        private readonly IMarketplaceSaasResourceDataSource _dataSource;
        private readonly ILogger _logger;
        private readonly IMarketplaceARMClient _marketplaceARMClient;
        private readonly IIfxAuditLogger _ifxAuditLogger;

        public MarketplaceResourceController(
            IMarketplaceSaasResourceDataSource resourceMetadataEntityDataSource,
            ILogger logger,
            IMarketplaceARMClient marketplaceARMClient,
            IIfxAuditLogger ifxAuditLogger)
        {
            _dataSource = resourceMetadataEntityDataSource;
            _logger = logger;
            _marketplaceARMClient = marketplaceARMClient;
            _ifxAuditLogger = ifxAuditLogger;
        }

        // PUT api/resourceMetadata/{resourceId}/{subscriptionId}
        [SwaggerOperation(OperationId = "Put")]
        [HttpPut("{resourceId}/{marketplaceSubId}")]
        public async Task<IActionResult> AddMarketplaceResourceAsync(string resourceId, string marketplaceSubId)
        {
            var marketplaceSubscription = MarketplaceSubscription.From(marketplaceSubId);
            var saasResource = new MarketplaceSaasResourceEntity(marketplaceSubscription, new MarketplaceSubscriptionDetailsEntity()
            {
                Name = "test-name",
                PlanId = "planid",
                OfferId = "offerId",
                PublisherId = "publisherId",
                Beneficiary = new SaasBeneficiary() { TenantId = "tenantId" },
                Id = marketplaceSubId
            },
            BillingTermTypes.Monthly);

            var addedEntity = await _dataSource.AddAsync(saasResource);
            _logger.Information("Added entity with marketplace subscription: {@marketplaceSubscription}", addedEntity.MarketplaceSubscription);
            _ifxAuditLogger.LogAudit(
                    new IFxAuditCallerId[] { new IFxAuditCallerId { Type = IFxAuditCallerIdType.TENANT_ID, Value = "tenantId" }, },
                    IFxAuditEventCategories.RESORUCE_MANAGEMENT,
                    $"Put",
                    new IFxAuditTargetResource[] { new IFxAuditTargetResource() { Name = resourceId, Type = Constants.DatadogResourceProvider } },
                    IFxAuditResultType.SUCCESS);
            return Ok(addedEntity);
        }

        // GET api/resourceMetadata/{subscriptionId}
        [SwaggerOperation(OperationId = "Get")]
        [HttpGet("{marketplaceSubId}")]
        public async Task<IActionResult> GetMarketplaceResourceAsync(string marketplaceSubId)
        {
            var entity = await _dataSource.GetAsync(MarketplaceSubscription.From(marketplaceSubId));
            _logger.Information("Got entity with marketplace subscription id: {@mpSub}", entity.MarketplaceSubscription);
            _ifxAuditLogger.LogAudit(
                    new IFxAuditCallerId[] { new IFxAuditCallerId { Type = IFxAuditCallerIdType.TENANT_ID, Value = "tenantId" }, },
                    IFxAuditEventCategories.RESORUCE_MANAGEMENT,
                    $"Get",
                    new IFxAuditTargetResource[] { new IFxAuditTargetResource() { Name = marketplaceSubId, Type = Constants.DatadogResourceProvider } },
                    IFxAuditResultType.SUCCESS);
            return Ok(entity);
        }

        [SwaggerOperation(OperationId = "Get")]
        [HttpGet("billing/listsaasresources")]
        public async Task<SaasResourcesListResponse> ListSaasResourcesAsync([FromQuery(Name = "token")] string token = null, [FromQuery(Name = "page-size")] int pageSize = 10)
        {
            DateTime? timeStamp = null;
            string continuationToken = null;

            var Request = HttpContext.Request;
            string baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";

            if (!string.IsNullOrEmpty(token))
            {
                timeStamp = new DateTime(Convert.ToInt64(token, CultureInfo.InvariantCulture), DateTimeKind.Utc);
            }

            var paginatedResponse = await _dataSource.GetPaginatedResourcesAsync(pageSize, timeStamp);

            if (paginatedResponse.LastTimeStamp != null)
            {
                continuationToken = paginatedResponse.LastTimeStamp.Value.Ticks.ToString(CultureInfo.InvariantCulture);
            }

            var response = new SaasResourcesListResponse
            {
                Subscriptions = paginatedResponse.Entities.Select(entity => entity.SubscriptionDetails),
                NextLink = string.IsNullOrEmpty(continuationToken) ? null : (baseUrl + '?' + "token=" + continuationToken + "&page-size=" + paginatedResponse.PageSize)
            };
            return response;
        }

        [HttpPut("subscriptionlevel/{resourceGroup}/{resourceName}")]
        public async Task<MarketplaceSubscriptionDetails> CreateSubLevelSaas(string resourceGroup, string resourceName)
        {
            // var subscriptionId = "52d42ba4-3473-4064-9f95-e780df01f6de";
            var subId = "d3c0b378-d50b-4ac7-ac42-b9aacc66f6c5";
            var saasResourceProperties = new MarketplaceSaasResourceProperties()
            {
                Name = resourceName,
                PaymentChannelMetadata = new PaymentChannelMetadata()
                {
                    AzureSubscriptionId = subId,
                },
                PlanId = "payg",
                PublisherId = "datadog1591740804488",
                PaymentChannelType = "SubscriptionDelegated",
                OfferId = "dd_liftr_v2",
                TermId = "hjdtn7tfnxcy"
            };

            var requestMetadata = new MarketplaceRequestMetadata()
            {
                MSClientTenantId = "6457aa98-4dba-4966-a260-6fc215e8616a",
                MSClientObjectId = "25f0ce98-7b18-4510-9966-6f97f27667cf",
                // MSClientPrincipalId = "10030000A5D03A4B",
                // MSClientIssuer = "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/",
                MSClientPrincipalName = "runnerDF@billtest434458live.ccsctp.net",
            };

            var response = await _marketplaceARMClient.CreateSaaSResourceAsync(saasResourceProperties, requestMetadata, resourceGroup);
            return response;
        }

        [HttpDelete("subscriptionlevel/{resourceGroup}/{resourceName}")]
        public async Task DeleteSubLevelSaas(string resourceGroup, string resourceName)
        {
            var requestMetadata = new MarketplaceRequestMetadata()
            {
                MSClientTenantId = "6457aa98-4dba-4966-a260-6fc215e8616a",
                MSClientObjectId = "25f0ce98-7b18-4510-9966-6f97f27667cf",
                // MSClientPrincipalId = "10030000A5D03A4B",
                // MSClientIssuer = "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/",
                MSClientPrincipalName = "akagarw@microsoft.com",
            };

            await _marketplaceARMClient.DeleteSaaSResourceAsync("d3c0b378-d50b-4ac7-ac42-b9aacc66f6c5", resourceName, resourceGroup, requestMetadata);
        }

        [HttpPut("tenantlevel/{resourceName}")]
        public async Task<MarketplaceSubscriptionDetails> CreateTenantLevelSaas(string resourceName)
        {
            var subId = "d3c0b378-d50b-4ac7-ac42-b9aacc66f6c5";
            var saasResourceProperties = new MarketplaceSaasResourceProperties()
            {
                Name = resourceName,
                PaymentChannelMetadata = new PaymentChannelMetadata()
                {
                    AzureSubscriptionId = subId,
                },
                PlanId = "payg",
                PublisherId = "datadog1591740804488",
                PaymentChannelType = "SubscriptionDelegated",
                OfferId = "dd_liftr_v2",
                TermId = "hjdtn7tfnxcy"
            };

            var requestMetadata = new MarketplaceRequestMetadata()
            {
                MSClientTenantId = "6457aa98-4dba-4966-a260-6fc215e8616a",
                MSClientObjectId = "25f0ce98-7b18-4510-9966-6f97f27667cf",
                // MSClientPrincipalId = "10030000A5D03A4B",
                // MSClientIssuer = "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/",
                MSClientPrincipalName = "runnerDF@billtest434458live.ccsctp.net",
            };

            var response = await _marketplaceARMClient.CreateSaaSResourceAsync(saasResourceProperties, requestMetadata);
            return response;
        }

        [HttpDelete("tenantlevel/{marketplaceSubscription}")]
        public async Task DeleteTenantLevelSaas(string marketplaceSubscription)
        {
            var requestMetadata = new MarketplaceRequestMetadata()
            {
                MSClientTenantId = "6457aa98-4dba-4966-a260-6fc215e8616a",
                MSClientObjectId = "25f0ce98-7b18-4510-9966-6f97f27667cf",
                // MSClientPrincipalId = "10030000A5D03A4B",
                // MSClientIssuer = "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/",
                MSClientPrincipalName = "runnerDF@billtest434458live.ccsctp.net",
            };

            await _marketplaceARMClient.DeleteSaaSResourceAsync(MarketplaceSubscription.From(marketplaceSubscription), requestMetadata);
        }
    }
}