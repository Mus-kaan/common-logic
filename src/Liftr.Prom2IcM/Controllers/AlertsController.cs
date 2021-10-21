//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Liftr.Prom2IcM.Examples;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Prom2IcM.Controllers
{
    /// <summary>
    /// This handles webhook from Prometheus alert manager. Please see <see cref="WebhookMessage"/> for the webhook contract.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class AlertsController : ControllerBase
    {
        private readonly AlertRelay _alertRelay;
        private readonly Serilog.ILogger _logger;

        public AlertsController(AlertRelay alertRelay, Serilog.ILogger logger)
        {
            _alertRelay = alertRelay;
            _logger = logger;
        }

        [HttpPost]
        [SwaggerOperation(OperationId = "Post")]
        [SwaggerRequestExample(typeof(WebhookMessage), typeof(WebhookMessageExample))]
        public async Task PostAsync(WebhookMessage webhookMessage)
        {
            // TODO: remove this detailed payload logging after this is very stable.
            _logger.Information("webhookMessage: {@webhookMessage}", webhookMessage);
            await _alertRelay.GenerateIcMIncidentAsync(webhookMessage);
        }
    }
}
