//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Logging.AspNetCore;

namespace Microsoft.Liftr.Billing.Web
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            WebHost
                .CreateDefaultBuilder(args)
                 .UseLiftrLogger()
                 .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
