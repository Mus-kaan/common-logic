//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.IdempotentRPWorker.Interfaces;
using System.Collections.Generic;

namespace Microsoft.Liftr.IdempotentRPWorker.Service
{
    public class WorkerCommandToObjectMapper : IWorkerCommandToObjectMapper<RPWorkerQueueCommandEnum>
    {
        public WorkerCommandToObjectMapper()
        {
            WorkerCommandToObjectDictionary = new Dictionary<RPWorkerQueueCommandEnum, IWorkerCommandProcessor>();
        }

        private Dictionary<RPWorkerQueueCommandEnum, IWorkerCommandProcessor> WorkerCommandToObjectDictionary { get; }

        public IWorkerCommandProcessor GetObject(RPWorkerQueueCommandEnum workerCommand)
        {
            return WorkerCommandToObjectDictionary[workerCommand];
        }

        public void SetObject(RPWorkerQueueCommandEnum workerCommand, IWorkerCommandProcessor commandObject)
        {
            WorkerCommandToObjectDictionary.Add(workerCommand, commandObject);
        }
    }
}
