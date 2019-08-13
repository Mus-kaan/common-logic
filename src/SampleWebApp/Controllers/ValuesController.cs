//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Liftr.Logging.AspNetCore;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SampleWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly ILogger _logger;

        public ValuesController(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            Log();
            Log();
            var client = new HttpClient();
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
#pragma warning disable CA2234 // Pass system uri objects instead of strings
            client.GetAsync("https://msazure.visualstudio.com").Wait();
#pragma warning restore CA2234 // Pass system uri objects instead of strings
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()

            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            var client = new HttpClient();
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
#pragma warning disable CA2234 // Pass system uri objects instead of strings
            client.GetAsync("https://msazure.visualstudio.com").Wait();
#pragma warning restore CA2234 // Pass system uri objects instead of strings
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
            Log();
            return "value";
        }

        // POST api/values
        [HttpPost]
        public ValueRequest Post([FromBody] ValueRequest req)
        {
            return req;
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        private void Log()
        {
            _logger.Fatal("This is a Fatal log.");
            _logger.Error("This is a Error log.");
            _logger.Warning("This is a Warning log.");
            _logger.Information("This is a Information log.");
            _logger.Verbose("This is a Verbose log.");
            _logger.Debug("This is a Debug log.");
        }
    }
}
