//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.IdempotentRPWorker.MarketplaceStates;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.IdempotentRPWorker.Service
{
    public class IdempotentBuilder<T> where T : BaseResource
    {
        private Queue<IState<StatesEnum, StateContext>> _stateQueue;
        private RPWorkerDataBuilder _builderData;
        private T _resource;

        public IdempotentBuilder(RPWorkerDataBuilder builderData, T resource)
        {
            _stateQueue = new Queue<IState<StatesEnum, StateContext>>();
            _builderData = builderData ?? throw new ArgumentNullException(nameof(builderData));
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
        }

        public void AddStateToQueue(IState<StatesEnum, StateContext> state)
        {
            _stateQueue.Enqueue(state);
        }

        public void AddCreateSaaSStateToQueue()
        {
            var createSaasState = new CreateSaaS(_builderData.MarketplaceBuilder.MarketplaceARMClient, _builderData.MarketplaceBuilder.MarketplaceFulfillmentClient, _builderData.MarketplaceBuilder.SignAgreementService, _builderData.Logger, _builderData.MarketplaceBuilder.SaaSClientHack);
            _stateQueue.Enqueue(createSaasState);
        }

        public void AddActivateSaaSStateToQueue()
        {
            var activateSaasState = new ActivateSaaS(_builderData.MarketplaceBuilder.MarketplaceFulfillmentClient, _builderData.MarketplaceBuilder.MarketplaceARMClient, _builderData.Logger, _builderData.MarketplaceBuilder.SaaSClientHack);
            _stateQueue.Enqueue(activateSaasState);
        }

        public void AddDeleteSaaSStateToQueue()
        {
            var deleteSaasState = new DeleteSaaS(_builderData.MarketplaceBuilder.MarketplaceFulfillmentClient, _builderData.MarketplaceBuilder.MarketplaceARMClient, _builderData.Logger, _builderData.MarketplaceBuilder.SaaSClientHack);
            _stateQueue.Enqueue(deleteSaasState);
        }

        public IdempotentOrchestrator<T> Build()
        {
            return new IdempotentOrchestrator<T>(_builderData, _stateQueue, _resource);
        }
    }
}
