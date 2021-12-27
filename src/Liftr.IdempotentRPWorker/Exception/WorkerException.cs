//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using System;

namespace Microsoft.Liftr.IdempotentRPWorker
{
    public class WorkerException : Exception
    {
        public WorkerException(string message)
            : base(message)
        {
        }

        public WorkerException(string message, string statusCode)
           : base(message)
        {
            StatusCode = statusCode;
        }

        public WorkerException(string message, IState<StatesEnum, StateContext> state)
            : base(message)
        {
            State = state;
        }

        public WorkerException(string message, Exception innerException, IState<StatesEnum, StateContext> state)
            : base(message, innerException)
        {
            State = state;
        }

        public WorkerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public WorkerException(string message, Exception innerException, string statusCode)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public WorkerException()
        {
        }

        public IState<StatesEnum, StateContext> State { get; set; }

        public string StatusCode { get; set; }
    }
}
