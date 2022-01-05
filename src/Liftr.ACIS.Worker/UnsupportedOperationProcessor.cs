//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Worker
{
    public class UnsupportedOperationProcessor : IACISOperationProcessor
    {
        private readonly ILogger _logger;
        private readonly string _loggerPrefix = $"[Worker] [ACIS]";

        public UnsupportedOperationProcessor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessAsync(ACISOperationRequest request)
        {
            request = request ?? throw new ArgumentNullException(nameof(request));
            var message = $"Unsupported Operation Name. OperationName: {request.OperationName} for operationId {request.OperationId}";
            await request.Operation.SuccessfulFinishAsync(message);
            _logger.Warning($"{_loggerPrefix} {message}");
            throw new InvalidOperationException(message);
        }
    }
}
