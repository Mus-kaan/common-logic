using Microsoft.AspNetCore.Mvc;
using Microsoft.Liftr.Contracts.ARM;
using Microsoft.Liftr.MarketplaceResource.DataSource.Interfaces;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Liftr.Sample.Web.Controllers
{
    [ApiController]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class MockResourceController : ControllerBase
    {
        private readonly ILogger _logger;

        public MockResourceController(
            ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// List all monitors under the specified resource group.
        /// </summary>
        [HttpGet]
        [Route(DatadogRPConstants.MonitorsByResourceGroupRoute)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ListResponse<DatadogMonitorResource>))]
        [SwaggerOperation(OperationId = "Monitors_ListByResourceGroup")]
        public async Task<ListResponse<DatadogMonitorResource>> ListByResourceGroupAsync(
            string subscriptionId, string resourceGroupName)
        {
            _logger.Information("ListByResourceGroupAsync: " + resourceGroupName);
            var list = await Task.FromResult(new ListResponse<DatadogMonitorResource>());
            return list;
        }

        /// <summary>
        /// Validate the parameters for creating/updating a monitor resource.
        /// </summary>
        [HttpPost]
        [Route(DatadogRPConstants.MonitorCreationValidateRoute)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ValidationResponse> CreationValidateAsync(
            string subscriptionId, string resourceGroupName, string monitorName, DatadogMonitorResource resource)
        {
            await Task.Yield();
            _logger.Information("CreationValidateAsync: " + monitorName);
            return ValidationResponse.BuildValidationResponseSuccessful();
        }

        /// <summary>
        /// Create a monitor resource.
        /// </summary>
        [HttpPut]
        [Route(DatadogRPConstants.MonitorRoute)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DatadogMonitorResource))]
        [ProducesResponseType((int)HttpStatusCode.Created, Type = typeof(DatadogMonitorResource))]
        [SwaggerOperation(OperationId = "Monitors_Create")]
        public async Task<ActionResult<DatadogMonitorResource>> CreateAsync(
            string subscriptionId, string resourceGroupName, string monitorName, DatadogMonitorResource resource)
        {
            await Task.Yield();
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (monitorName == null)
            {
                throw new ArgumentNullException(nameof(monitorName));
            }

            if (monitorName.Contains("1"))
            {
                return BadRequest();
            }
            else if (monitorName.Contains("2"))
            {
                throw new InvalidOperationException("Testing unhandled exception");
            }

            var operationId = Guid.NewGuid().ToString();
            var operationStatusUri = new Uri("https://api-dogfood.resources.windows-int.net/" + operationId);
            _logger.Information("Started creating resource with Id '{resourceId}', operationStatusUri: '{operationStatusUri}'", resource.Id, operationStatusUri.ToString());
            return Created(operationStatusUri, resource);
        }
    }
}