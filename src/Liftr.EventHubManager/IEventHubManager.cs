//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using System.Collections.Generic;

namespace Microsoft.Liftr.EventHubManager
{
    public interface IEventHubManager
    {
        public IEventHubEntity Get(string location);

        public List<IEventHubEntity> Get(string location, uint count);

        public List<IEventHubEntity> GetAll(string location);
    }
}
