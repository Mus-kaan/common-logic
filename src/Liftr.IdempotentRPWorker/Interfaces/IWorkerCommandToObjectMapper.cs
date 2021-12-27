//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.IdempotentRPWorker.Interfaces
{
    /// <summary>
    /// This Interface represents contract for Worker Command enum to object map
    /// </summary>
    /// <typeparam name="TCommand">Enum that stores the various commands of RP Worker</typeparam>
    public interface IWorkerCommandToObjectMapper<TCommand>
    {
        IWorkerCommandProcessor GetObject(TCommand workerCommand);

        void SetObject(TCommand workerCommand, IWorkerCommandProcessor commandObject);
    }
}
