//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using System;

namespace Microsoft.Liftr.IdempotentRPWorker.Service
{
    public static class IdempotentBuilderExtensions
    {
        public static IdempotentBuilder<T> WithSaaSCreate<T>(this IdempotentBuilder<T> idempotentBuilder, IState<StatesEnum, StateContext> createSaaS) where T : BaseResource
        {
            Validate(idempotentBuilder, createSaaS);

            idempotentBuilder.AddStateToQueue(createSaaS);
            return idempotentBuilder;
        }

        public static IdempotentBuilder<T> WithSaaSCreateImplemented<T>(this IdempotentBuilder<T> idempotentBuilder) where T : BaseResource
        {
            Validate(idempotentBuilder);

            idempotentBuilder.AddCreateSaaSStateToQueue();
            return idempotentBuilder;
        }

        public static IdempotentBuilder<T> WithSaaSActivate<T>(this IdempotentBuilder<T> idempotentBuilder, IState<StatesEnum, StateContext> activateSaaS) where T : BaseResource
        {
            Validate(idempotentBuilder, activateSaaS);

            idempotentBuilder.AddStateToQueue(activateSaaS);
            return idempotentBuilder;
        }

        public static IdempotentBuilder<T> WithSaaSActivateImplemented<T>(this IdempotentBuilder<T> idempotentBuilder) where T : BaseResource
        {
            Validate(idempotentBuilder);

            idempotentBuilder.AddActivateSaaSStateToQueue();
            return idempotentBuilder;
        }

        public static IdempotentBuilder<T> WithPartnerSignup<T>(this IdempotentBuilder<T> idempotentBuilder, IState<StatesEnum, StateContext> partnerSignup) where T : BaseResource
        {
            Validate(idempotentBuilder, partnerSignup);

            idempotentBuilder.AddStateToQueue(partnerSignup);
            return idempotentBuilder;
        }

        public static IdempotentBuilder<T> WithDeleteSaaS<T>(this IdempotentBuilder<T> idempotentBuilder, IState<StatesEnum, StateContext> deleteSaaS) where T : BaseResource
        {
            Validate(idempotentBuilder, deleteSaaS);

            idempotentBuilder.AddStateToQueue(deleteSaaS);
            return idempotentBuilder;
        }

        public static IdempotentBuilder<T> WithDeleteSaaSImplemented<T>(this IdempotentBuilder<T> idempotentBuilder) where T : BaseResource
        {
            Validate(idempotentBuilder);

            idempotentBuilder.AddDeleteSaaSStateToQueue();
            return idempotentBuilder;
        }

        public static IdempotentBuilder<T> WithLinkOrg<T>(this IdempotentBuilder<T> idempotentBuilder, IState<StatesEnum, StateContext> linkOrg) where T : BaseResource
        {
            Validate(idempotentBuilder, linkOrg);

            idempotentBuilder.AddStateToQueue(linkOrg);
            return idempotentBuilder;
        }

        public static IdempotentBuilder<T> WithDeleteOrg<T>(this IdempotentBuilder<T> idempotentBuilder, IState<StatesEnum, StateContext> deleteOrg) where T : BaseResource
        {
            Validate(idempotentBuilder, deleteOrg);

            idempotentBuilder.AddStateToQueue(deleteOrg);
            return idempotentBuilder;
        }

        public static IdempotentBuilder<T> WithCreateUserAccount<T>(this IdempotentBuilder<T> idempotentBuilder, IState<StatesEnum, StateContext> createUserAccount) where T : BaseResource
        {
            Validate(idempotentBuilder, createUserAccount);

            idempotentBuilder.AddStateToQueue(createUserAccount);
            return idempotentBuilder;
        }

        public static IdempotentBuilder<T> WithCreateIngestionAPIKey<T>(this IdempotentBuilder<T> idempotentBuilder, IState<StatesEnum, StateContext> createIngestionAPIKey) where T : BaseResource
        {
            Validate(idempotentBuilder, createIngestionAPIKey);

            idempotentBuilder.AddStateToQueue(createIngestionAPIKey);
            return idempotentBuilder;
        }

        public static IdempotentBuilder<T> WithCustomState<T>(this IdempotentBuilder<T> idempotentBuilder, IState<StatesEnum, StateContext> customState) where T : BaseResource
        {
            Validate(idempotentBuilder, customState);

            idempotentBuilder.AddStateToQueue(customState);
            return idempotentBuilder;
        }

        private static void Validate<T>(IdempotentBuilder<T> idempotentBuilder, IState<StatesEnum, StateContext> currentState) where T : BaseResource
        {
            if (idempotentBuilder == null)
            {
                throw new ArgumentNullException(nameof(idempotentBuilder));
            }

            if (currentState == null)
            {
                throw new ArgumentNullException(nameof(currentState));
            }
        }

        private static void Validate<T>(IdempotentBuilder<T> idempotentBuilder) where T : BaseResource
        {
            if (idempotentBuilder == null)
            {
                throw new ArgumentNullException(nameof(idempotentBuilder));
            }
        }
    }
}
