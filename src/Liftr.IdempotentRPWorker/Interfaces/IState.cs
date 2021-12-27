//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using System.Threading.Tasks;

namespace Microsoft.Liftr.IdempotentRPWorker
{
    /// <summary>
    /// This Interface represents blueprint of a state that every state implementation should follow
    /// </summary>
    /// <typeparam name="TState">Enum that stores the various states of the idempotent operation</typeparam>
    /// <typeparam name="TStateContext">State Context that for resource Idempotent flow</typeparam>
    public interface IState<TState, TStateContext>
    {
        TState State { get; set; }

        bool IsSucessfullyExecuted { get; set; }

        bool IsRetryable { get; set; }

        Task<TStateContext> ExecuteAsync(TStateContext stateContext, BaseResource resource);

        Task<TStateContext> RollbackAsync(TStateContext stateContext, BaseResource resource);
    }
}
