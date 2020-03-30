//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Liftr;
using Microsoft.Liftr.Contracts.ARM;
using Microsoft.Liftr.RPaaS;
using Newtonsoft.Json;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
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
        private readonly IMetaRPStorageClient _metaRPStorageClient;

        public ValuesController(Serilog.ILogger logger, IMetaRPStorageClient metaRPStorageClient)
        {
            _logger = logger;
            _metaRPStorageClient = metaRPStorageClient;
        }

        // GET api/values/ListAllResources
        [HttpGet("ListAllResources")]
        [SwaggerOperation(OperationId = "List")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "<Pending>")]
        public async Task<IEnumerable<ARMResource>> GetListAsync()
        {
            _logger.Information($"{nameof(GetListAsync)} start");

            try
            {
                var apiVersion = "2019-11-01-preview";
                var requestPath = "/subscriptions/60d3e394-7bbe-4744-a115-363c94f9a209/providers/Microsoft.IncrediBuild/clusters";
                return await _metaRPStorageClient.ListResourcesAsync<TestResource>(requestPath, apiVersion);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw;
            }
        }

        // GET api/values/ListAllSubResources
        [HttpGet("ListAllSubResources")]
        [SwaggerOperation(OperationId = "ListSubResources")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "<Pending>")]
        public async Task<IEnumerable<ARMResource>> GetMultiListAsync()
        {
            _logger.Information($"{nameof(GetListAsync)} start");

            try
            {
                var apiVersion = "2020-02-01-preview";
                var userRpSubscription = "f9d7ebed-adbd-4cb4-b973-aaf82c136138";
                var providerName = "Microsoft.Datadog";

                await _metaRPStorageClient.GetTenantForSubscriptionAsync(userRpSubscription, providerName, userRpSubscription, apiVersion);
                await _metaRPStorageClient.GetTenantForAllSubscriptionsAsync(userRpSubscription, providerName, apiVersion);

                var requestPath = "/subscriptions/f9d7ebed-adbd-4cb4-b973-aaf82c136138/resourceGroups/limgurg/providers/Microsoft.Datadog/monitors/testdf12";
                var resource = new TestResource()
                {
                    Id = requestPath,
                    Location = "West US",
                    Name = "testdf12",
                    Type = "Microsoft.Datadog",
                };

                await _metaRPStorageClient.UpdateResourceAsync(resource, requestPath, apiVersion);

                return await _metaRPStorageClient.ListResourcesAsync<TestResource>(requestPath, apiVersion);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw;
            }
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
        public async Task<ValueRequest> PostAsync([FromBody] ValueRequest req)
        {
            var resourceId = "/subscriptions/f9aed45d-b9e6-462a-a3f5-6ab34857bc17/resourceGroups/myrg/providers/Microsoft.Nginx/frontends/frontend";
            var apiVersion = "2019-11-01-preview";
            try
            {
                var resource = await _metaRPStorageClient.GetResourceAsync<ARMResource>(resourceId, apiVersion);
                Console.WriteLine(JsonConvert.SerializeObject(resource));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw;
            }

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

    public class TestResource : ARMResource
    {
        public override string Type { get; set; }
    }
}
