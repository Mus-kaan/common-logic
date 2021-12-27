//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.IdempotentRPWorker;
using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using System.Threading.Tasks;

namespace Liftr.IdempotentRPWorker.Tests
{
    public class TestState : IState<StatesEnum, StateContext>
    {
        public StatesEnum State { get; set; }

        public bool IsSucessfullyExecuted { get; set; }

        public bool IsRetryable { get; set; }

        public async Task<StateContext> ExecuteAsync(StateContext stateContext, BaseResource resource)
        {
            await Task.CompletedTask;
            return stateContext;
        }

        public async Task<StateContext> RollbackAsync(StateContext stateContext, BaseResource resource)
        {
            await Task.CompletedTask;
            return stateContext;
        }
    }
}
