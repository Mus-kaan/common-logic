//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.StatusStore
{
    public interface IStatusReader
    {
        public Task<IList<IStatusRecord>> GetStateAsync(string key, string machineName = null);

        public Task<IList<IStatusRecord>> GetHistoryAsync(string key = null);
    }
}
