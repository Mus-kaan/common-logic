//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Liftr.Logging.AspNetCore;
using Microsoft.Liftr.Metrics.Prom;
using Microsoft.Liftr.WebHosting;

namespace Microsoft.Liftr.Sample.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost
                .CreateDefaultBuilder(args)
                .UseLiftrLogger()
                .UsePrometheusMetrics()
                .UseKeyVaultProvider("SampleRP")
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
