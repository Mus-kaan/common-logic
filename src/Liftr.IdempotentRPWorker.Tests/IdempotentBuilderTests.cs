//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.IdempotentRPWorker;
using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.IdempotentRPWorker.Interfaces;
using Microsoft.Liftr.IdempotentRPWorker.Service;
using Microsoft.Liftr.IdempotentRPWorker.Utils;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Liftr.IdempotentRPWorker.Tests
{
    public class IdempotentBuilderTests
    {
        [Fact]
        public void Add_State_To_Queue_And_Builder_Expected_Behavior()
        {
            var state = new TestState();
            RPWorkerDataBuilder builderData = new RPWorkerDataBuilder()
            {
                StateContext = new StateContext(StatesEnum.Intialized, 1),
            };
            BaseResource resource = new BaseResource()
            {
                Name = "testResource",
                Id = "id1",
            };

            var builder = new IdempotentBuilder<BaseResource>(builderData, resource);
            builder.AddStateToQueue(state);
            var idempotentOrchestrator = builder.Build();
            Assert.NotNull(idempotentOrchestrator);
        }

        [Fact]
        public void Builder_Throws_Exception_When_BuilderData_Is_Null()
        {
            var state = new TestState();
            RPWorkerDataBuilder builderData = null;
            BaseResource resource = new BaseResource()
            {
                Name = "testResource",
                Id = "id1",
            };

            Assert.Throws<ArgumentNullException>(() => new IdempotentBuilder<BaseResource>(builderData, resource));
        }

        [Fact]
        public void Builder_Throws_Exception_When_WorkerQueue_Is_Null()
        {
            var state = new TestState();
            RPWorkerDataBuilder builderData = new RPWorkerDataBuilder()
            {
                StateContext = new StateContext(StatesEnum.Intialized, 1),
            };
            BaseResource resource = null;

            Assert.Throws<ArgumentNullException>(() => new IdempotentBuilder<BaseResource>(builderData, resource));
        }
    }
}
