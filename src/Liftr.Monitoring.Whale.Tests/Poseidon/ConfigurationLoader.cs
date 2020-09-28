//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.TokenManager;
using System.IO;

namespace Microsoft.Liftr.Monitoring.Whale.Poseidon.Tests
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
        /// Provider for a test  API key.
        /// </summary>
        public static string GetAPIKey()
        {
            var configuration = GetConfigurationRoot();
            var apiKey = configuration["ApiKey"];
            return apiKey;
        }

        /// <summary>
        /// Load configurations from appsettings.json and user secrets file.
        /// </summary>
        private static IConfigurationRoot GetConfigurationRoot()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("0c742241-55ac-4059-beab-1f3b82797c7e")
                .Build();
        }
    }
}
