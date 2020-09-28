//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.Whale.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Whale.Interfaces
{
    public interface IWhaleFilterClient
    {
        Task<IEnumerable<MonitoredResource>> ListResourcesByTagsAsync(
            string subscriptionId, string tenantId, IEnumerable<FilteringTag> filteringTags);
    }
}
