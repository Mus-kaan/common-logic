//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Liftr.Configuration
{
    public static class ConfigurationExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Middleware should fail silently.")]
        public static bool ContainsSerilogWriteToConsole(this IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            try
            {
                var writeTos = configuration.GetSection("Serilog")?.GetSection("WriteTo")?.GetChildren();
                if (writeTos == null)
                {
                    return false;
                }

                foreach (var writeTo in writeTos)
                {
                    var name = writeTo.GetSection("Name")?.Value;
                    if (name.OrdinalContains("Console"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
