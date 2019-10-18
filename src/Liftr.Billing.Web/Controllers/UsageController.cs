//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Liftr.Billing;
using System;
using System.Net;
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
            var usageRecordEntity = UsageRecordEntity.From(usageEvent);
            if (!await _pushAgentClient.TryInsertSingleUsageAsync(usageRecordEntity, cancellationToken))
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }
    }
}
