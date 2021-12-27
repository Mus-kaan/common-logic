//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.IdempotentRPWorker;
using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.IdempotentRPWorker.Interfaces;
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
    public class IdempotentHelperTests
    {
        private readonly Mock<IWorkerDatabaseService> _mockDbSvc;

        public IdempotentHelperTests()
        {
            _mockDbSvc = new Mock<IWorkerDatabaseService>();
        }

        [Fact]
        public async Task Execute_States_In_Queue_Expected_BehaviorAsync()
        {
            Queue<IState<StatesEnum, StateContext>> statesInQueue = new Queue<IState<StatesEnum, StateContext>>();
            Stack<IState<StatesEnum, StateContext>> statesExecuted = new Stack<IState<StatesEnum, StateContext>>();
            StateContext stateContext = new StateContext(StatesEnum.Intialized, 1);
            var resource = new TestResource()
            {
                Name = "test",
            };
            var logger = new LoggerConfiguration()
                       .WriteTo.Console()
                       .CreateLogger();
            var state = new TestState();
            statesInQueue.Enqueue(state);
            statesInQueue.Enqueue(state);

            using var httpResponseMessage = new HttpResponseMessage();

            _mockDbSvc
               .Setup(x => x.PatchResourceAsync(It.IsAny<TestResource>(), It.IsAny<StateContext>()))
               .ReturnsAsync(httpResponseMessage);

            var output = await IdempotentHelper.ExecuteStatesInQueueAsync(statesInQueue, statesExecuted, stateContext, resource, logger, _mockDbSvc.Object, state);
            var expectedOutput = output as TestResource;

            Assert.True(statesInQueue.Count == 0);
            Assert.True(statesExecuted.Count == 2);
            Assert.Equal(resource.Name, expectedOutput.Name);
        }

        [Fact]
        public async Task Rollback_States_In_Stack_Expected_BehaviorAsync()
        {
            Stack<IState<StatesEnum, StateContext>> statesExecuted = new Stack<IState<StatesEnum, StateContext>>();
            StateContext stateContext = new StateContext(StatesEnum.Intialized, 1);
            var resource = new TestResource()
            {
                Name = "test",
            };
            var logger = new LoggerConfiguration()
                       .WriteTo.Console()
                       .CreateLogger();
            var state = new TestState();
            statesExecuted.Push(state);
            statesExecuted.Push(state);

            using var httpResponseMessage = new HttpResponseMessage();

            _mockDbSvc
               .Setup(x => x.PatchResourceAsync(It.IsAny<TestResource>(), It.IsAny<StateContext>()))
               .ReturnsAsync(httpResponseMessage);

            await IdempotentHelper.RollbackStatesInStackAsync(statesExecuted, stateContext, resource, _mockDbSvc.Object, logger);

            Assert.True(statesExecuted.Count == 0);
        }

        [Fact]
        public async Task Execute_States_In_Queue_Throws_Exception_When_Queue_Is_Null_Async()
        {
            Queue<IState<StatesEnum, StateContext>> statesInQueue = null;
            Stack<IState<StatesEnum, StateContext>> statesExecuted = new Stack<IState<StatesEnum, StateContext>>();
            StateContext stateContext = new StateContext(StatesEnum.Intialized, 1);
            var resource = new TestResource()
            {
                Name = "test",
            };
            var logger = new LoggerConfiguration()
                       .WriteTo.Console()
                       .CreateLogger();
            var state = new TestState();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await IdempotentHelper.ExecuteStatesInQueueAsync(statesInQueue, statesExecuted, stateContext, resource, logger, _mockDbSvc.Object, state));
        }

        [Fact]
        public async Task Execute_States_In_Queue_Throws_Exception_When_Stack_Is_Null_Async()
        {
            Queue<IState<StatesEnum, StateContext>> statesInQueue = new Queue<IState<StatesEnum, StateContext>>();
            Stack<IState<StatesEnum, StateContext>> statesExecuted = null;
            StateContext stateContext = new StateContext(StatesEnum.Intialized, 1);
            var resource = new TestResource()
            {
                Name = "test",
            };
            var logger = new LoggerConfiguration()
                       .WriteTo.Console()
                       .CreateLogger();
            var state = new TestState();
            statesInQueue.Enqueue(state);
            statesInQueue.Enqueue(state);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await IdempotentHelper.ExecuteStatesInQueueAsync(statesInQueue, statesExecuted, stateContext, resource, logger, _mockDbSvc.Object, state));
        }

        [Fact]
        public async Task Execute_States_In_Queue_Throws_Exception_When_StateContext_Is_Null_Async()
        {
            Queue<IState<StatesEnum, StateContext>> statesInQueue = new Queue<IState<StatesEnum, StateContext>>();
            Stack<IState<StatesEnum, StateContext>> statesExecuted = new Stack<IState<StatesEnum, StateContext>>();
            StateContext stateContext = null;
            var resource = new TestResource()
            {
                Name = "test",
            };
            var logger = new LoggerConfiguration()
                       .WriteTo.Console()
                       .CreateLogger();
            var state = new TestState();
            statesInQueue.Enqueue(state);
            statesInQueue.Enqueue(state);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await IdempotentHelper.ExecuteStatesInQueueAsync(statesInQueue, statesExecuted, stateContext, resource, logger, _mockDbSvc.Object, state));
        }

        [Fact]
        public async Task Execute_States_In_Queue_Throws_Exception_When_DBSvc_Is_Null_Async()
        {
            Queue<IState<StatesEnum, StateContext>> statesInQueue = new Queue<IState<StatesEnum, StateContext>>();
            Stack<IState<StatesEnum, StateContext>> statesExecuted = new Stack<IState<StatesEnum, StateContext>>();
            StateContext stateContext = new StateContext(StatesEnum.Intialized, 1);
            var resource = new TestResource()
            {
                Name = "test",
            };
            var logger = new LoggerConfiguration()
                       .WriteTo.Console()
                       .CreateLogger();
            var state = new TestState();
            statesInQueue.Enqueue(state);
            statesInQueue.Enqueue(state);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await IdempotentHelper.ExecuteStatesInQueueAsync(statesInQueue, statesExecuted, stateContext, resource, logger, null, state));
        }

        [Fact]
        public async Task Rollback_States_In_Stack_Throws_Exception_When_Stack_Is_Null_Async()
        {
            Stack<IState<StatesEnum, StateContext>> statesExecuted = null;
            StateContext stateContext = new StateContext(StatesEnum.Intialized, 1);
            var resource = new TestResource()
            {
                Name = "test",
            };
            var logger = new LoggerConfiguration()
                       .WriteTo.Console()
                       .CreateLogger();
            var state = new TestState();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await IdempotentHelper.RollbackStatesInStackAsync(statesExecuted, stateContext, resource, _mockDbSvc.Object, logger));
        }

        [Fact]
        public async Task Rollback_States_In_Stack_Throws_Exception_When_Logger_Is_Null_Async()
        {
            Stack<IState<StatesEnum, StateContext>> statesExecuted = new Stack<IState<StatesEnum, StateContext>>();
            StateContext stateContext = new StateContext(StatesEnum.Intialized, 1);
            var resource = new TestResource()
            {
                Name = "test",
            };
            var state = new TestState();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await IdempotentHelper.RollbackStatesInStackAsync(statesExecuted, stateContext, resource, _mockDbSvc.Object, null));
        }
    }
}
