//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource
{
    public interface ICounterEntityDataSource
    {
        Task IncreaseCounterAsync(string counterName, int incrementValue = 1);

        Task<int?> GetCounterAsync(string counterName);

        Task<IDictionary<string, int>> ListCountersAsync(string prefix = null);
    }
}
