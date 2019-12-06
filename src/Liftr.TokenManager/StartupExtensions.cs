//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Liftr.TokenManager
{
    public static class StartupExtensions
    {
        public static void AddTokenManager(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<TokenManagerConfiguration>(configuration.GetSection(nameof(TokenManagerConfiguration)));
            services.AddSingleton<ITokenManager, TokenManager>();
        }
    }
}
