//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Billing.Web.Controllers
{
    [ApiController]
    public class UsageController : Controller
    {
        private readonly PushAgentClient _pushAgentClient;
        private readonly Serilog.ILogger _logger;

        public UsageController(IBillingServiceProvider billingServiceProvider, Serilog.ILogger logger)
        {
            if (billingServiceProvider is null)
            {
                throw new ArgumentNullException(nameof(billingServiceProvider));
            }

            _pushAgentClient = billingServiceProvider.GetPushAgentClient();
            _logger = logger;
        }

        [HttpPost]
        [Route("api/usageEvent")]
        public async Task<IActionResult> PostUsageEventAsync([FromBody] UsageEvent usageEvent, CancellationToken cancellationToken)
        {
            if (!await _pushAgentClient.TryInsertSingleUsageAsync(usageEvent, cancellationToken))
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }

        [HttpPost]
        [Route("api/batchUsageEvent")]
        public async Task<IActionResult> PostBatchUsageEventAsync([FromBody] BatchUsageEvent batchUsageEvent, CancellationToken cancellationToken)
        {
            if (batchUsageEvent == null || !batchUsageEvent.UsageEvents.Any() || batchUsageEvent.UsageEvents.Count() > TableConstants.TableServiceBatchMaximumOperations)
            {
                return StatusCode(StatusCodes.Status400BadRequest, $"Batch size should be between 1 and {TableConstants.TableServiceBatchMaximumOperations}");
            }

            if (!await _pushAgentClient.TryInsertBatchUsageAsync(batchUsageEvent, cancellationToken))
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }
    }
}
