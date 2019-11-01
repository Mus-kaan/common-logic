//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Liftr;
using Microsoft.Liftr.Logging.AspNetCore;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SampleWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
    public class ValuesController : ControllerBase
    {
        private static int s_cnt = 0;
        private readonly ILogger _logger;

        public ValuesController(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        // GET api/values
        [HttpGet]
        [SwaggerOperation(OperationId = "List")]
        public ActionResult<IEnumerable<string>> GetList()
        {
            Log();
            Log();
            var client = new HttpClient();
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
#pragma warning disable CA2234 // Pass system uri objects instead of strings
            client.GetAsync("https://msazure.visualstudio.com").Wait();
            DoWorkAsync().Wait();
#pragma warning restore CA2234 // Pass system uri objects instead of strings
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()

            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        [SwaggerOperation(OperationId = "Get")]
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
        [SwaggerOperation(OperationId = "Post")]
        public ValueRequest Post([FromBody] ValueRequest req)
        {
            return req;
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        [ProducesResponseType((int)HttpStatusCode.Created, Type = typeof(int))]
        [SwaggerOperation(OperationId = "Put")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(void))]
        [SwaggerOperation(OperationId = "Delete")]
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

        private async Task DoWorkAsync()
        {
            s_cnt++;
            _logger.Information("Timed Background Service is working.");
            using (var op = _logger.StartTimedOperation("GetMSWebPage"))
            {
                op.SetContextProperty("CntVal", s_cnt);
                try
                {
                    _logger.Information("Before start http get.");
                    await Task.Delay(300);
                    if (s_cnt % 3 == 2)
                    {
                        throw new InvalidOperationException($"num: {s_cnt}");
                    }

                    using (var client = new HttpClient())
                    {
#pragma warning disable CA2234 // Pass system uri objects instead of strings
                        var result = await client.GetStringAsync("https://microsoft.com");
#pragma warning restore CA2234 // Pass system uri objects instead of strings
                        _logger.Debug("Respose length: {length}", result.Length);
                        op.SetProperty("ResponseLength", result.Length);
                        op.SetResultDescription("Get ms web succeed.");
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    _logger.Error(ex, "Do work failed.");
                    op.FailOperation("Do work failed.");
                    throw;
                }
            }
        }
    }
}
