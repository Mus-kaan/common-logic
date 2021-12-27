//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    public sealed class StateContext
    {
        public StateContext(
           StatesEnum currentState,
           int workerRetryCount)
        {
            State = currentState;
            WorkerRetryCount = workerRetryCount;
            StateExecutionResults = new Dictionary<string, bool>();
            ExtendedContext = new Dictionary<object, object>();
        }

        public RPWorkerQueueCommandEnum WorkerCommand { get; private set; }

        public string TenantId { get;  set; }

        public string SubscriptionId { get; set; }

        public StatesEnum State { get; private set; }

        public StatesEnum PreviousState { get; private set; }

        public string ResourceId { get; set; }

        public PartnerRequestMetaData RequestMetadata { get; private set; }

        public string ExceptionMessage { get; private set; } = string.Empty;

        public string ApiVersion { get; private set; }

        public int WorkerRetryCount { get; private set; }

        public bool IsRetryRequire { get; private set; } = true;

        public bool IsSaaSDeleted { get; private set; } = false;

        public bool IsSaaSActivated { get; private set; } = false;

        public Dictionary<string, bool> StateExecutionResults { get; private set; }

        public MarketplaceContext MarketplaceContext { get; private set; }

        public Dictionary<object, object> ExtendedContext { get; set; }

        public void UpdateNextState(StatesEnum state)
        {
            PreviousState = State;
            State = state;
        }

        public void UpdateRequestMetadata(PartnerRequestMetaData requestMetadata)
        {
            RequestMetadata = requestMetadata;
        }

        public void UpdateAPIVersion(string apiVersion)
        {
            ApiVersion = apiVersion;
        }

        public void UpdateExceptionMessage(Exception exception)
        {
            ExceptionMessage = exception?.Message;
        }

        public void UpdateRetryRequire(bool isRetryRequire)
        {
            IsRetryRequire = isRetryRequire;
        }

        public void SetSaaSDeleteStatus(bool isSaasDeleted)
        {
            IsSaaSDeleted = isSaasDeleted;
        }

        public void SetSaaSActivationStatus(bool isSaaSActivated)
        {
            IsSaaSActivated = isSaaSActivated;
        }

        public void UpdateWorkerRetryCount(int retryCount)
        {
            WorkerRetryCount = retryCount;
        }

        public void UpdateWorkerCommand(RPWorkerQueueCommandEnum workerCommand)
        {
            WorkerCommand = workerCommand;
        }

        public void UpdateStateExecutionResult(string state, bool IsSucess)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            StateExecutionResults[state] = IsSucess;
        }

        public void UpdateMarketplaceContext(MarketplaceContext marketplaceContext)
        {
            MarketplaceContext = marketplaceContext;
        }
    }
}
