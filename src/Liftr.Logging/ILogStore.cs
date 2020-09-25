//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Logging
{
    public interface ILogStore
    {
        Task<Uri> UploadLogAsync(string logContent, CancellationToken cancellationToken = default);

        Task<Uri> UploadLogAsync(Stream logContent, CancellationToken cancellationToken = default);
    }
}
