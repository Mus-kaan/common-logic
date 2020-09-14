//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using JustEat.StatsD;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Microsoft.Liftr.Logging.Metrics
{
    /// <summary>
    /// JustEat.StatsD is used as the statsD client
    /// Here's the link to JustEat.StatsD:https://github.com/justeat/JustEat.StatsD
    /// </summary>
    public class MetricSender : IMetricSender
    {
        private readonly ILogger _logger;

        private readonly string _defaultNamespace;

        private readonly IStatsDPublisher _publisher;
        private readonly Dictionary<string, string> _defaultDimensions;

        public MetricSender(
            string host,
            string defaultNamespace,
            ILogger logger,
            Dictionary<string, string> defaultDimensions,
            int port = 8125)
        {
            if (string.IsNullOrWhiteSpace(defaultNamespace))
            {
                throw new ArgumentNullException(nameof(defaultNamespace));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.Information("StatsDPublisher init with host: {host}, port: {port}", host, port);

            _defaultNamespace = defaultNamespace;

            _defaultDimensions = defaultDimensions;

            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.Warning("host is null. Create a dummy null metric sender.");
                return;
            }

            _publisher = new StatsDPublisher(new StatsDConfiguration()
            {
                Host = host,
                Port = port,
                OnError = (ex) =>
                {
                    if (ex is SocketException)
                    {
                        _logger.Warning(ex, "warn_statsDClient_SocketException. Socket exception on StatsD client.");
                    }
                    else
                    {
                        _logger.Error(ex, "err_statsDClient_UnknownException. Exception on StatsD client.");
                    }

                    // https://github.com/justeat/JustEat.StatsD
                    // Return true if the exception was handled and no further action is needed
                    return true;
                },
            });
        }

        public void Gauge(string metric, int value, Dictionary<string, string> dimension = null)
        {
            if (_publisher == null)
            {
                return;
            }

            var dims = dimension ?? _defaultDimensions;
            Gauge(_defaultNamespace, metric, value, dims);
        }

        public void Gauge(string mdmNamespace, string metric, int value, Dictionary<string, string> dimension = null)
        {
            if (_publisher == null)
            {
                return;
            }

            Dictionary<string, string> dims;
            if (dimension != null && _defaultDimensions != null)
            {
                dims = new Dictionary<string, string>();

                foreach (var dimensionPairs in _defaultDimensions)
                {
                    dims[dimensionPairs.Key] = dimensionPairs.Value;
                }

                foreach (var dimensionPairs in dimension)
                {
                    dims[dimensionPairs.Key] = dimensionPairs.Value;
                }
            }
            else
            {
                dims = dimension ?? _defaultDimensions;
            }

            _publisher.Gauge(Convert.ToDouble(value), new Bucket(mdmNamespace, metric, dims).ToJson());
        }
    }
}
