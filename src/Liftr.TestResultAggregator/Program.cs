//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Liftr.Logging.AspNetCore;
using Microsoft.Liftr.Metrics.Prom;

namespace Microsoft.Liftr.TestResultAggregator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost
                .CreateDefaultBuilder(args)
                .UseLiftrLogger()
                .UsePrometheusMetrics()
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
