//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.StatusStore
{
    public interface IStatusStore : IStatusReader
    {
        public Task<Uri> UpdateStateAsync(string key, string value, CancellationToken cancellationToken = default);

        Task<IStatusRecord> GetCurrentMachineStateAsync(string key);
    }
}
