//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.IdempotentRPWorker.Interfaces;
using System.Collections.Generic;

namespace Microsoft.Liftr.IdempotentRPWorker.Service
{
    public class StateToObjectMapper<TStateContext> : IStateToObjectMapper<StatesEnum, TStateContext>
    {
        public StateToObjectMapper()
        {
            StateToObjectDictionary = new Dictionary<StatesEnum, IState<StatesEnum, TStateContext>>();
        }

        private Dictionary<StatesEnum, IState<StatesEnum, TStateContext>> StateToObjectDictionary { get; }

        public IState<StatesEnum, TStateContext> GetStateObject(StatesEnum state)
        {
            return StateToObjectDictionary[state];
        }

        public void SetStateObject(StatesEnum state, IState<StatesEnum, TStateContext> stateObject)
        {
            StateToObjectDictionary[state] = stateObject;
        }
    }
}
