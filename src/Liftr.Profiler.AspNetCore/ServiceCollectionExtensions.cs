//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Liftr.Profiler.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppInsightsProfiler(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var ikey = configuration.GetSection("ApplicationInsights")?.GetSection("InstrumentationKey")?.Value;
            if (string.IsNullOrEmpty(ikey))
            {
                return serviceCollection;
            }

            var enableStr = configuration.GetSection("ApplicationInsights")?.GetSection("EnableProfiler")?.Value;
            if (bool.TryParse(enableStr, out var enable))
            {
                if (enable)
                {
                    Console.WriteLine("Enable Application Insights Profiler ...");
                    serviceCollection.AddServiceProfiler();
                }
            }

            return serviceCollection;
        }
    }
}
