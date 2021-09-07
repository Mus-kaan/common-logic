//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.TokenManager;
using System.IO;

namespace Microsoft.Liftr.Datadog.Whale.Poseidon
{
    /// <summary>
    /// Provider for dedicated test configurations.
    /// </summary>
    public static class ConfigurationLoader
    {
        /// <summary>
        /// Provider for MonitoringSvcMongoOptions.
        /// </summary>
        public static MonitoringSvcMongoOptions GetMongoOptions()
        {
            var mongoOptions = new MonitoringSvcMongoOptions();

            var configuration = GetConfigurationRoot();
            configuration
                .GetSection(nameof(MonitoringSvcMongoOptions))
                .Bind(mongoOptions);

            mongoOptions.ConnectionString = TestDBConnection.TestMongodbConStr;

            return mongoOptions;
        }

        /// <summary>
        /// Provider for TokenManagerConfiguration.
        /// </summary>
        public static TokenManagerConfiguration GetTokenManagerConfiguration()
        {
            var tokenManagerConfiguration = new TokenManagerConfiguration();

            var configuration = GetConfigurationRoot();
            configuration
                .GetSection(nameof(TokenManagerConfiguration))
                .Bind(tokenManagerConfiguration);

            return tokenManagerConfiguration;
        }

        /// <summary>
        /// Provider for a test Datadog API key.
        /// </summary>
        public static string GetDatadogApiKey()
        {
            var configuration = GetConfigurationRoot();
            var datadogApiKey = configuration["DatadogApiKey"];

            return datadogApiKey ?? "dummyApiKey";
        }

        /// <summary>
        /// Provider for a test Datadog application key.
        /// </summary>
        public static string GetDatadogApplicationKey()
        {
            var configuration = GetConfigurationRoot();
            var datadogApplicationKey = configuration["DatadogApplicationKey"];

            return datadogApplicationKey ?? "dummyApplicationKey";
        }

        /// <summary>
        /// Load configurations from appsettings.json and user secrets file.
        /// </summary>
        private static IConfigurationRoot GetConfigurationRoot()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("a230081a-dccc-49c9-ba47-651d22e0bd9a")
                .Build();
        }
    }
}
