//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Platform.Contracts.Interfaces
{
    public interface IRestSharpService<TEntity, TResult>
    {
        Task<TResult> CreateAsync(TEntity entity, IDictionary<string, string> headers, Uri endpoint, IDictionary<string, string> parameters);

        Task<TResult> GetAsync(IDictionary<string, string> headers, Uri endpoint, IDictionary<string, string> parameters);

        Task<TResult> UpdateAsync(TEntity entity, IDictionary<string, string> headers, Uri endpoint, IDictionary<string, string> parameters);
    }
}
