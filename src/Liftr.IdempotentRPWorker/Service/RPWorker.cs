//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr;
using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using System;

namespace Microsoft.Liftr.IdempotentRPWorker.Service
{
    public static class RPWorker
    {
        public static IdempotentBuilder<T> CreateIdempotentBuilder<T>(T resource, RPWorkerDataBuilder builderData) where T : BaseResource
        {
            if (builderData == null)
            {
                throw new ArgumentNullException(nameof(builderData));
            }

            if (!IsValid(builderData))
            {
                throw new ArgumentNullException(nameof(builderData));
            }

            return new IdempotentBuilder<T>(builderData, resource);
        }

        private static bool IsValid(RPWorkerDataBuilder builderData)
        {
            return builderData != null
                && builderData.QueueMessage != null
                && builderData.QueueReaderOptions != null
                && builderData.DbSvc != null
                && builderData.CancellationToken != null;
        }
    }
}
