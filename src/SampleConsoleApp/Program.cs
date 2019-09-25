//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.GenericHosting;
using Microsoft.Liftr.Logging.GenericHosting;
using System.IO;

namespace GenericHostSample
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                    configHost.AddEnvironmentVariables(prefix: "PREFIX_");
                    configHost.AddCommandLine(args);
                })
                .UseDefaultAppConfigWithKeyVault(keyVaultPrefix: "IncrediBuildRP")
                .UseLiftrLogger()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<LifetimeEventsHostedService>();
                    services.AddHostedService<TimedHostedService>();
                })
                .UseConsoleLifetime()
                .Build();

            host.Run();
        }
    }
}
