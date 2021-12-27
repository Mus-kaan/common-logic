//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.IdempotentRPWorker.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.IdempotentRPWorker.Utils
{
    public static class IdempotentHelper
    {
        public static async Task<T> ExecuteStatesInQueueAsync<T>(Queue<IState<StatesEnum, StateContext>> statesInQueue, Stack<IState<StatesEnum, StateContext>> statesExecuted, StateContext stateContext, T resource, ILogger logger, IWorkerDatabaseService dbSvc, IState<StatesEnum, StateContext> state) where T : BaseResource
        {
            ValidateParams(statesExecuted, stateContext, resource, dbSvc, logger);
            bool IsStateExecutedSuccessfully = true;

            if (statesInQueue == null)
            {
                throw new ArgumentNullException(nameof(statesInQueue));
            }

            while (!AreAllStatesExecuted(statesInQueue))
            {
                state = statesInQueue.Dequeue();
                if (state == null)
                {
                    throw new InvalidOperationException($"{nameof(ExecuteStatesInQueueAsync)} State is found null in the Queue during execution!!");
                }

                logger.Information($"{nameof(ExecuteStatesInQueueAsync)} Executing state {state} for the resource {resource}");
                stateContext = await state.ExecuteAsync(stateContext, resource);

                logger.Information($"{nameof(ExecuteStatesInQueueAsync)} State {state} execution result is: {IsStateExecutedSuccessfully} . Updating in the database for the resource {resource}");
                await dbSvc.PatchResourceAsync(resource, stateContext);
                logger.Information($"{nameof(ExecuteStatesInQueueAsync)} State {state} updated successfully in the database for the resource {resource}");

                statesExecuted.Push(state);
            }

            return resource;
        }

        public static async Task RollbackStatesInStackAsync(Stack<IState<StatesEnum, StateContext>> statesExecuted, StateContext stateContext, BaseResource resource, IWorkerDatabaseService dbSvc, ILogger logger)
        {
            ValidateParams(statesExecuted, stateContext, resource, dbSvc, logger);

            while (!IsRollbackCompleted(statesExecuted))
            {
                var state = statesExecuted.Pop();

                if (state == null)
                {
                    throw new InvalidOperationException($"{nameof(RollbackStatesInStackAsync)} State is found null in the Stack during Rollback!!");
                }

                logger.Information($"{nameof(RollbackStatesInStackAsync)} Rollingback state {state} for the resource {resource}");
                stateContext = await state.RollbackAsync(stateContext, resource);
                logger.Information($"{nameof(RollbackStatesInStackAsync)} Rollingback of state {state} is successful for the resource {resource}");

                logger.Information($"{nameof(RollbackStatesInStackAsync)} State {state} is successfully Rolled Back. Updating in the database for the resource {resource}");
                await dbSvc.PatchResourceAsync(resource, stateContext);
                logger.Information($"{nameof(RollbackStatesInStackAsync)} State {state} updated successfully in the database for the resource {resource}");
            }
        }

        private static void ValidateParams(Stack<IState<StatesEnum, StateContext>> statesExecuted, StateContext stateContext, BaseResource resource, IWorkerDatabaseService dbSvc, ILogger logger)
        {
            if (stateContext == null)
            {
                throw new ArgumentNullException(nameof(stateContext));
            }

            if (resource is null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (statesExecuted == null)
            {
                throw new ArgumentNullException(nameof(statesExecuted));
            }

            if (dbSvc == null)
            {
                throw new ArgumentNullException(nameof(dbSvc));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
        }

        private static bool IsRollbackCompleted(Stack<IState<StatesEnum, StateContext>> statesExecuted)
        {
            return statesExecuted.Count == 0 ? true : false;
        }

        private static bool AreAllStatesExecuted(Queue<IState<StatesEnum, StateContext>> statesInQueue)
        {
            return statesInQueue.Count == 0 ? true : false;
        }
    }
}
