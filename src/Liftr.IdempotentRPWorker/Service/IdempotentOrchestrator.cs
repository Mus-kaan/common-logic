//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.IdempotentRPWorker.Interfaces;
using Microsoft.Liftr.IdempotentRPWorker.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.IdempotentRPWorker.Service
{
    public sealed class IdempotentOrchestrator<T> : IDisposable where T : BaseResource
    {
        private readonly SemaphoreSlim _tokenSemaphore = new SemaphoreSlim(1, 1);
        private Queue<IState<StatesEnum, StateContext>> _statesInQueue;
        private Stack<IState<StatesEnum, StateContext>> _statesExecuted;
        private RPWorkerDataBuilder _builderData;
        private T _resource;

        public IdempotentOrchestrator(RPWorkerDataBuilder builderData, Queue<IState<StatesEnum, StateContext>> stateQueue, T resource)
        {
            _builderData = builderData ?? throw new ArgumentNullException(nameof(builderData));
            _statesInQueue = stateQueue ?? throw new ArgumentNullException(nameof(stateQueue));
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
            _statesExecuted = new Stack<IState<StatesEnum, StateContext>>();
        }

        public void Dispose()
        {
            _tokenSemaphore.Dispose();
        }

        public async Task<WorkerResults> RunAsync()
        {
            var stateContext = _builderData.StateContext;
            var dbSvc = _builderData.DbSvc;
            var logger = _builderData.Logger;
            string errorMessage = string.Empty;
            bool isStateFailed = false;
            IState<StatesEnum, StateContext> state = null;
            Exception exception = null;
            WorkerResults workerResults = null;

            await _tokenSemaphore.WaitAsync();

            try
            {
                logger.Information($"{nameof(RunAsync)} Starting Idempotent Execution of all the {_statesInQueue.Count} states for the resource {_resource.Id} for command: {_builderData.StateContext.WorkerCommand}");

                _resource = await IdempotentHelper.ExecuteStatesInQueueAsync(_statesInQueue, _statesExecuted, stateContext, _resource, logger, dbSvc, state);

                _builderData.StateContext = stateContext;
                logger.Information($"{nameof(RunAsync)} Completed Idempotent Execution of all the states for the resource {_resource.Id}");

                workerResults = new WorkerResults()
                {
                    UpdatedResource = _resource,
                    UpdatedStateContext = stateContext,
                };

                return workerResults;
            }
            catch (Exception ex)
            {
                errorMessage = $"{nameof(RunAsync)} failed with Error: {ex.Message}";
                logger.Error(ex, errorMessage);
                isStateFailed = true;
                exception = ex;
                state = _statesExecuted.Count > 0 ? _statesExecuted.Peek() : null;
            }
            finally
            {
                _tokenSemaphore.Release();
            }

            if (isStateFailed && state != null)
            {
                try
                {
                    await RollbackAsync(stateContext, _resource, state, dbSvc, logger);
                }
                catch (Exception ex)
                {
                    errorMessage = $"{nameof(RunAsync)} State: {state} failed with Error: {errorMessage} and Rollback failed with Error: {ex.Message}";
                    logger.Error(ex, errorMessage);
                }

                throw new WorkerException(stateContext?.ExceptionMessage, exception, state);
            }

            return workerResults;
        }

        private async Task RollbackAsync(StateContext stateContext, BaseResource resource, IState<StatesEnum, StateContext> currentState, IWorkerDatabaseService dbSvc, ILogger logger)
        {
            var maxDequeueCount = _builderData.QueueReaderOptions.MaxDequeueCount;
            var currentDequeueCount = stateContext.WorkerRetryCount;

            try
            {
                if (currentDequeueCount == maxDequeueCount)
                {
                    logger.Information($"{nameof(RollbackAsync)} Starting Rollback of the {_statesExecuted.Count} states for the resource {resource.Id}");
                    await IdempotentHelper.RollbackStatesInStackAsync(_statesExecuted, stateContext, resource, dbSvc, logger);
                }
                else
                {
                    logger.Information($"{nameof(RollbackAsync)} Rollingback state {currentState} for the resource {resource.Id}");
                    await currentState.RollbackAsync(stateContext, resource);
                    _statesExecuted.Clear();
                    logger.Information($"{nameof(RollbackAsync)} Rollingback of state {currentState} is successful for the resource {resource.Id}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{nameof(RollbackAsync)} Rollback failed with the message: {ex.Message}");
            }
        }
    }
}
