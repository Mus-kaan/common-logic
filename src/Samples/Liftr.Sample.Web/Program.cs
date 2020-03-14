//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Liftr.Logging.AspNetCore;
using Microsoft.Liftr.WebHosting;

namespace Liftr.Sample.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost
                .CreateDefaultBuilder(args)
                .UseLiftrLogger()
                .UseKeyVaultProvider("SampleRP")
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
