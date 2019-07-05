//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public sealed class MongoCollectionsFactory
    {
        public MongoCollectionsFactory(MongoOptions options, ILogger logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new ArgumentException($"Need valid {nameof(options.ConnectionString)}.");
            }

            if (string.IsNullOrEmpty(options.DatabaseName))
            {
                throw new ArgumentException($"Need valid {nameof(options.DatabaseName)}.");
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var mongoUrl = new MongoUrl(options.ConnectionString);
            var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);

            if (options.LogDBOperation)
            {
                mongoClientSettings.ClusterConfigurator = clusterConfigurator =>
                {
                    clusterConfigurator.Subscribe<CommandSucceededEvent>(e =>
                    {
                        _logger.Verbose("[Mongo | CommandSucceeded] Event :{@CommandSucceededEvent}, Start at UTC: " + DateTime.Now.Subtract(e.Duration).ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), e);
                    });

                    clusterConfigurator.Subscribe<CommandFailedEvent>(e =>
                    {
                        _logger.Verbose("[Mongo | CommandFailed] Event :{@CommandFailedEvent}, Start at UTC: " + DateTime.Now.Subtract(e.Duration).ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), e);
                    });
                };
            }

            var client = new MongoClient(mongoClientSettings);
        }

        #region Private
        private readonly ILogger _logger;
        #endregion
    }
}
