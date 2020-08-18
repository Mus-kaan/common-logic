using Microsoft.AspNetCore.Mvc;
using Microsoft.IFxAudit;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.IFxAuditLinux;
using Microsoft.Liftr.MarketplaceResource.DataSource.Interfaces;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace Liftr.Sample.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketplaceResourceController : ControllerBase
    {
        private readonly IMarketplaceResourceContainerEntityDataSource _dataSource;
        private readonly ILogger _logger;
        private readonly IIfxAuditLogger _ifxAuditLogger;

        public MarketplaceResourceController(
            IMarketplaceResourceContainerEntityDataSource resourceMetadataEntityDataSource,
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
            var saasResource = new MarketplaceSaasResourceEntity(marketplaceSubscription, "test-name", "planid", "offerId", "publisherId", "hjdtn7tfnxcy", BillingTermTypes.Monthly, new SaasBeneficiary() { TenantId = "tenantId" });

            var entity = new MarketplaceResourceContainerEntity(saasResource, resourceId, "tenantId");
            var addedEntity = await _dataSource.AddAsync(entity);
            _logger.Information("Added entity with resource id: {resourceId}", entity.ResourceId);
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
            var entity = await _dataSource.GetEntityForMarketplaceSubscriptionAsync(MarketplaceSubscription.From(marketplaceSubId));
            _logger.Information("Got entity with marketplace subscription id: {@mpSub}", entity.MarketplaceSaasResource.MarketplaceSubscription);
            _ifxAuditLogger.LogAudit(
                    new IFxAuditCallerId[] { new IFxAuditCallerId { Type = IFxAuditCallerIdType.TENANT_ID, Value = "tenantId" }, },
                    IFxAuditEventCategories.RESORUCE_MANAGEMENT,
                    $"Get",
                    new IFxAuditTargetResource[] { new IFxAuditTargetResource() { Name = marketplaceSubId, Type = Constants.DatadogResourceProvider } },
                    IFxAuditResultType.SUCCESS);
            return Ok(entity);
        }
    }
}