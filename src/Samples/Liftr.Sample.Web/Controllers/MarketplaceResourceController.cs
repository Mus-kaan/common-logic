using Microsoft.AspNetCore.Mvc;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
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

        public MarketplaceResourceController(
            IMarketplaceResourceContainerEntityDataSource resourceMetadataEntityDataSource,
            ILogger logger)
        {
            _dataSource = resourceMetadataEntityDataSource;
            _logger = logger;
        }

        // PUT api/resourceMetadata/{resourceId}/{subscriptionId}
        [SwaggerOperation(OperationId = "Put")]
        [HttpPut("{resourceId}/{marketplaceSubId}")]
        public async Task<IActionResult> AddMarketplaceResourceAsync(string resourceId, string marketplaceSubId)
        {
            var marketplaceSubscription = MarketplaceSubscription.From(marketplaceSubId);
            var saasResource = new MarketplaceSaasResourceEntity(marketplaceSubscription, "test-name", "planid", "hjdtn7tfnxcy", BillingTermTypes.Monthly);

            var entity = new MarketplaceResourceContainerEntity(saasResource, resourceId, "tenantId");
            var addedEntity = await _dataSource.AddAsync(entity);
            _logger.Information("Added entity with resource id: {resourceId}", entity.ResourceId);
            return Ok(addedEntity);
        }

        // GET api/resourceMetadata/{subscriptionId}
        [SwaggerOperation(OperationId = "Get")]
        [HttpGet("{marketplaceSubId}")]
        public async Task<IActionResult> GetMarketplaceResourceAsync(string marketplaceSubId)
        {
            var entity = await _dataSource.GetEntityForMarketplaceSubscriptionAsync(MarketplaceSubscription.From(marketplaceSubId));
            _logger.Information("Got entity with marketplace subscription id: {@mpSub}", entity.MarketplaceSaasResource.MarketplaceSubscription);
            return Ok(entity);
        }
    }
}