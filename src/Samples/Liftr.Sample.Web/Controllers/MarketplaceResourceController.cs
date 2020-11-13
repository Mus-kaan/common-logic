using Microsoft.AspNetCore.Mvc;
using Microsoft.IFxAudit;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.IFxAuditLinux;
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
        private readonly IIfxAuditLogger _ifxAuditLogger;

        public MarketplaceResourceController(
            IMarketplaceSaasResourceDataSource resourceMetadataEntityDataSource,
            ILogger logger,
            IIfxAuditLogger ifxAuditLogger)
        {
            _dataSource = resourceMetadataEntityDataSource;
            _logger = logger;
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
                NextLink = string.IsNullOrEmpty(continuationToken) ? null : (baseUrl + '?' + "token=" + continuationToken)
            };
            return response;
        }
    }
}