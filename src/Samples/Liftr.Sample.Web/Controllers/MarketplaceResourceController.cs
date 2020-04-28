using Microsoft.AspNetCore.Mvc;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.MarketplaceResource.DataSource.Interfaces;
using Microsoft.Liftr.MarketplaceResource.DataSource.Models;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace Liftr.Sample.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketplaceResourceController : ControllerBase
    {
        private readonly IMarketplaceResourceEntityDataSource _dataSource;
        private readonly ILogger _logger;

        public MarketplaceResourceController(
            IMarketplaceResourceEntityDataSource resourceMetadataEntityDataSource,
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
            var entity = new MarketplaceResourceEntity(MarketplaceSubscription.From(marketplaceSubId), "saasResourceId", resourceId, "tenantId");
            var addedEntity = await _dataSource.AddEntityAsync(entity);
            _logger.Information("Added entity with resource id: {resourceId}", entity.ResourceId);
            return Ok(addedEntity);
        }

        // GET api/resourceMetadata/{resourceId}/{subscriptionId}
        [SwaggerOperation(OperationId = "Get")]
        [HttpGet("{marketplaceSubId}")]
        public async Task<IActionResult> GetMarketplaceResourceAsync(string marketplaceSubId)
        {
            var entity = await _dataSource.GetEntityForMarketplaceSubscriptionAsync(MarketplaceSubscription.From(marketplaceSubId));
            _logger.Information("Got entity with marketplace subscription id: {@mpSub}", entity.MarketplaceSubscription);
            return Ok(entity);
        }
    }
}