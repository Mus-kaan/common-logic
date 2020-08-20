//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.EventHubManager
{
    public class EventHubManagerSimple : IEventHubManager
    {
        private readonly IEventHubEntityDataSource _ehDatasource;
        private readonly Serilog.ILogger _logger;
        private static readonly TimeSpan s_refreshDuration = TimeSpan.FromMinutes(20);
        private MonitoringResourceProvider _provider;

        // RefreshEHAsync will NOT run concurrently so we do not need to lock
        // Also when update, we are assign _evhs to a new dict instead of in-place update, so theoretically we do not need to lock on read as well,
        // supposing the object pointer is returned atomically. It may access some stale entry but should be fine
        private Dictionary<string, List<IEventHubEntity>> _evhs = new Dictionary<string, List<IEventHubEntity>>();
        private Random _rand = new Random();

        public EventHubManagerSimple(IEventHubEntityDataSource ehDatasource, MonitoringResourceProvider provider, Serilog.ILogger logger)
        {
            _ehDatasource = ehDatasource ?? throw new ArgumentNullException(nameof(ehDatasource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _provider = provider;
            RefreshEHAsync().GetAwaiter().GetResult();
            var forget = PeriodicallyRefreshEHAsync();
        }

        public IEventHubEntity Get(string location)
        {
            return Get(location, 1)?[0];
        }

        public List<IEventHubEntity> Get(string location, uint count)
        {
            if (!_evhs.ContainsKey(location) || count == 0)
            {
                return null;
            }

            var eventHubs = _evhs[location];
            var eventHubCount = eventHubs.Count;
            if (count >= eventHubCount)
            {
                return eventHubs;
            }

            List<IEventHubEntity> result = new List<IEventHubEntity>();
            for (int i = 0; i < count; i++)
            {
                result.Add(eventHubs[_rand.Next(eventHubCount)]);
            }

            return result;
        }

        public List<IEventHubEntity> GetAll(string location)
        {
            if (!_evhs.ContainsKey(location))
            {
                return null;
            }

            return _evhs[location];
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private async Task PeriodicallyRefreshEHAsync()
        {
            while (true)
            {
                await Task.Delay(s_refreshDuration);
                await RefreshEHAsync();
            }
        }

        private async Task RefreshEHAsync()
        {
            try
            {
                var ehs = await _ehDatasource.ListAsync(_provider);

                var newDict = new Dictionary<string, List<IEventHubEntity>>();

                foreach (var eh in ehs)
                {
                    if (!newDict.ContainsKey(eh.Location))
                    {
                        newDict[eh.Location] = new List<IEventHubEntity>();
                    }

                    newDict[eh.Location].Add(eh);
                }

                _evhs = newDict;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(RefreshEHAsync));
                throw;
            }
        }
    }
}
