//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.IdempotentRPWorker;

namespace Microsoft.Liftr.IdempotentRPWorker.Interfaces
{
    /// <summary>
    /// This Interface represents contract for state enum to state object map
    /// </summary>
    /// <typeparam name="TState">Enum that stores the various stages of the idempotent operation</typeparam>
    /// <typeparam name="TStateContext">State Context that for resource Idempotent flow</typeparam>
    public interface IStateToObjectMapper<TState, TStateContext>
    {
        IState<TState, TStateContext> GetStateObject(TState state);

        void SetStateObject(TState state, IState<TState, TStateContext> stateObject);
    }
}
