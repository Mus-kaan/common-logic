//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Liftr.Sample.Web.Pages
{
    public class HealthCheckModel : PageModel
    {
        private readonly HealthCheckBackgroundService _healthCheckService;

        [BindProperty]
        public string IsHealthy { get; set; } = "true";

        public HealthCheckModel(HealthCheckBackgroundService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        public void OnPost()
        {
            if (IsHealthy.Equals("true"))
            {
                _healthCheckService.IsHealthy = true;
            }
            else
            {
                _healthCheckService.IsHealthy = false;
            }

            OnGet();
        }

        public void OnGet()
        {
            ViewData["pageMessage"] = $"IsHealthy: {_healthCheckService.IsHealthy}";
        }
    }
}
