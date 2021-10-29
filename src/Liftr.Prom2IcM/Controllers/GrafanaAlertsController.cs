//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Liftr.IcmConnector;
using Microsoft.Liftr.Prom2IcM.Examples;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Prom2IcM.Controllers
{
    /// <summary>
    /// This handles webhook from Grafana alert notification.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class GrafanaAlertsController : ControllerBase
    {
        private readonly AlertRelay _alertRelay;
        private readonly Serilog.ILogger _logger;

        public GrafanaAlertsController(AlertRelay alertRelay, Serilog.ILogger logger)
        {
            _alertRelay = alertRelay;
            _logger = logger;
        }

        [HttpPost]
        [SwaggerOperation(OperationId = "Post")]
        [SwaggerRequestExample(typeof(GrafanaWebhookMessage), typeof(GrafanaWebhookMessageExample))]
        public async Task PostAsync([FromBody] GrafanaWebhookMessage webhookMessage)
        {
            // TODO: remove this detailed payload logging after this is very stable.
            HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            using (StreamReader reader
                  = new StreamReader(HttpContext.Request.Body, Encoding.UTF8, true, 1024))
            {
                var bodyStr = await reader.ReadToEndAsync();
                _logger.Information("grafana webhookRequestBody: {grafanaWebhookRequestBody}", bodyStr);
            }

            _logger.Information("Parsed grafana webhookMessage: {@grafanaWebhookMessage}", webhookMessage);

            await _alertRelay.GenerateIcMFromGrafanaAsync(webhookMessage);
        }
    }
}
