//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Agreement.Interfaces
{
#nullable enable
    /// <summary>
    /// Sends Http Request for given request path and Http Method
    /// </summary>
    /// <returns>Generic T response</returns>
    /// <remarks>
    /// </remarks>
    public interface ISignAgreementRestClient
    {
        Task<T> SendRequestAsync<T>(
           HttpMethod method,
           string requestPath,
           Dictionary<string, string>? additionalHeaders = null,
           object? content = null,
           CancellationToken cancellationToken = default) where T : class;

        Task<T> SendRequestUsingTokenServiceAsync<T>(
          HttpMethod method,
          string requestPath,
          Dictionary<string, string>? additionalHeaders = null,
          object? content = null,
          CancellationToken cancellationToken = default) where T : class;
    }
#nullable disable
}
