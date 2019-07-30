//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Liftr.DiagnosticSource;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Logging.AspNetCore.Tests
{
    public sealed class LogFilterOverwriteMiddlewareTests
    {
        [Fact]
        public async Task CanSetByHeaderAsync()
        {
            CallContextHolder.CommonHttpHeaders.Value = new Dictionary<string, string>();
            var middleware = new LogFilterOverwriteMiddleware(next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
            });

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues("debug");
            contextMock.Setup(x => x.Request.Headers.TryGetValue("X-LIFTR-LOG-FILTER-OVERWRITE", out val)).Returns(true);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.Equal("debug", CallContextHolder.CommonHttpHeaders.Value["X-LIFTR-LOG-FILTER-OVERWRITE"]);
        }

        [Fact]
        public async Task LastHeaderWorkAsync()
        {
            CallContextHolder.CommonHttpHeaders.Value = new Dictionary<string, string>();
            var middleware = new LogFilterOverwriteMiddleware(next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
            });

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues(new string[] { "debug", "information" });
            contextMock.Setup(x => x.Request.Headers.TryGetValue("X-LIFTR-LOG-FILTER-OVERWRITE", out val)).Returns(true);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.Equal("information", CallContextHolder.CommonHttpHeaders.Value["X-LIFTR-LOG-FILTER-OVERWRITE"]);
        }

        [Fact]
        public async Task CanSetByQueryAsync()
        {
            CallContextHolder.CommonHttpHeaders.Value = new Dictionary<string, string>();
            var middleware = new LogFilterOverwriteMiddleware(next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
            });

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues("debug");
            contextMock.Setup(x => x.Request.Query.TryGetValue("LiftrLogFilterOverwrite", out val)).Returns(true);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.Equal("debug", CallContextHolder.CommonHttpHeaders.Value["X-LIFTR-LOG-FILTER-OVERWRITE"]);
        }

        [Fact]
        public async Task LastQueryWorkAsync()
        {
            CallContextHolder.CommonHttpHeaders.Value = new Dictionary<string, string>();
            var middleware = new LogFilterOverwriteMiddleware(next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
            });

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues(new string[] { "debug", "information" });
            contextMock.Setup(x => x.Request.Query.TryGetValue("LiftrLogFilterOverwrite", out val)).Returns(true);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.Equal("information", CallContextHolder.CommonHttpHeaders.Value["X-LIFTR-LOG-FILTER-OVERWRITE"]);
        }

        [Fact]
        public async Task QueryWinHeaderAsync()
        {
            CallContextHolder.CommonHttpHeaders.Value = new Dictionary<string, string>();
            var middleware = new LogFilterOverwriteMiddleware(next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
            });

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues(new string[] { "error", "fatal" });
            contextMock.Setup(x => x.Request.Headers.TryGetValue("X-LIFTR-LOG-FILTER-OVERWRITE", out val)).Returns(true);
            StringValues queryVal = new StringValues(new string[] { "debug", "verbose" });
            contextMock.Setup(x => x.Request.Query.TryGetValue("LiftrLogFilterOverwrite", out queryVal)).Returns(true);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.Equal("verbose", CallContextHolder.CommonHttpHeaders.Value["X-LIFTR-LOG-FILTER-OVERWRITE"]);
        }
    }
}
