//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.ACIS.Relay;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Management
{
    public class EventHubManagement
    {
        private readonly IEventHubEntityDataSource _evhDataSource;

        public EventHubManagement(IEventHubEntityDataSource evhDataSource)
        {
            _evhDataSource = evhDataSource ?? throw new ArgumentNullException(nameof(evhDataSource));
        }

        public async Task<IEnumerable<EventHubRecord>> ProcessListEventhubAsync(IACISOperation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            await operation.LogInfoAsync("Retrieving event hub meta data from RP DB ...");
            var list = await _evhDataSource.ListAsync();
            var result = list.Select(i => new EventHubRecord(i));

            await operation.SuccessfulFinishAsync(result.ToJson(indented: true));

            return result;
        }
    }
}
