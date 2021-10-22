//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Liftr.DiagnosticSource;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Logging.AspNetCore.Tests
{
    public sealed class LoggingMiddlewareTests
    {
        [Fact]
        public async Task CanSetByHeaderAsync()
        {
            string observeredValue = string.Empty;
            var middleware = new LoggingMiddleware(
                next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
                observeredValue = CallContextHolder.LogFilterOverwrite.Value;
            }, LoggerFactory.VoidLogger,
                logRequest: true,
                logSubdomain: false);

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues("debug");
            contextMock.Setup(x => x.Request.Headers.TryGetValue("X-Liftr-Log-Filter-Overwrite", out val)).Returns(true);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.Equal("debug", observeredValue);
        }

        [Fact]
        public async Task FirstHeaderWorkAsync()
        {
            string observeredValue = string.Empty;
            var middleware = new LoggingMiddleware(
                next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
                observeredValue = CallContextHolder.LogFilterOverwrite.Value;
            }, LoggerFactory.VoidLogger,
                logRequest: true,
                logSubdomain: false);

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues(new string[] { "debug", "information" });
            contextMock.Setup(x => x.Request.Headers.TryGetValue("X-Liftr-Log-Filter-Overwrite", out val)).Returns(true);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.Equal("debug", observeredValue);
        }

        [Fact]
        public async Task CanSetByQueryAsync()
        {
            string observeredValue = string.Empty;
            var middleware = new LoggingMiddleware(
                next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
                observeredValue = CallContextHolder.LogFilterOverwrite.Value;
            }, LoggerFactory.VoidLogger,
                logRequest: true,
                logSubdomain: false);

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues("debug");
            contextMock.Setup(x => x.Request.Query.TryGetValue("LiftrLogFilterOverwrite", out val)).Returns(true);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.Equal("debug", observeredValue);
        }

        [Fact]
        public async Task LastQueryWorkAsync()
        {
            string observeredValue = string.Empty;
            var middleware = new LoggingMiddleware(
                next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
                observeredValue = CallContextHolder.LogFilterOverwrite.Value;
            }, LoggerFactory.VoidLogger,
                logRequest: true,
                logSubdomain: false);

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues(new string[] { "debug", "information" });
            contextMock.Setup(x => x.Request.Query.TryGetValue("LiftrLogFilterOverwrite", out val)).Returns(true);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.Equal("information", observeredValue);
        }

        [Fact]
        public async Task QueryWinHeaderAsync()
        {
            string observeredValue = string.Empty;
            var middleware = new LoggingMiddleware(
                next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
                observeredValue = CallContextHolder.LogFilterOverwrite.Value;
            }, LoggerFactory.VoidLogger,
                logRequest: true,
                logSubdomain: false);

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues(new string[] { "error", "fatal" });
            contextMock.Setup(x => x.Request.Headers.TryGetValue("X-Liftr-Log-Filter-Overwrite", out val)).Returns(true);
            StringValues queryVal = new StringValues(new string[] { "debug", "verbose" });
            contextMock.Setup(x => x.Request.Query.TryGetValue("LiftrLogFilterOverwrite", out queryVal)).Returns(true);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.Equal("verbose", observeredValue);
        }

        [Fact]
        public async Task CanParseARMIdHeaderAsync()
        {
            string observeredFilter = string.Empty, observeredClientId = string.Empty, observeredTrackingId = string.Empty, observeredCorrelationId = string.Empty;
            var middleware = new LoggingMiddleware(
                next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
                observeredFilter = CallContextHolder.LogFilterOverwrite.Value;
                observeredClientId = CallContextHolder.ClientRequestId.Value;
                observeredTrackingId = CallContextHolder.ARMRequestTrackingId.Value;
                observeredCorrelationId = CallContextHolder.CorrelationId.Value;
            }, LoggerFactory.VoidLogger,
                logRequest: true,
                logSubdomain: false);

            var contextMock = new Mock<HttpContext>();

            var clientRId = Guid.NewGuid().ToString();
            var trackingId = Guid.NewGuid().ToString();
            var correalttionId = Guid.NewGuid().ToString();
            {
                StringValues val = new StringValues("debug");
                contextMock.Setup(x => x.Request.Headers.TryGetValue("X-Liftr-Log-Filter-Overwrite", out val)).Returns(true);
            }

            {
                StringValues val = new StringValues(clientRId);
                contextMock.Setup(x => x.Request.Headers.TryGetValue("X-MS-Client-Request-Id", out val)).Returns(true);
            }

            {
                StringValues val = new StringValues(trackingId);
                contextMock.Setup(x => x.Request.Headers.TryGetValue("X-MS-Arm-Request-Tracking-Id", out val)).Returns(true);
            }

            {
                StringValues val = new StringValues(correalttionId);
                contextMock.Setup(x => x.Request.Headers.TryGetValue("X-MS-Correlation-Request-Id", out val)).Returns(true);
            }

            await middleware.InvokeAsync(contextMock.Object);

            Assert.Equal("debug", observeredFilter);
            Assert.Equal(clientRId, observeredClientId);
            Assert.Equal(trackingId, observeredTrackingId);
            Assert.Equal(correalttionId, observeredCorrelationId);
        }

        [Fact]
        public async Task GenerateDefaultCorrelationIdAsync()
        {
            string observeredValue = string.Empty;
            var middleware = new LoggingMiddleware(
                next: async (innerHttpContext) =>
            {
                await Task.CompletedTask;
                observeredValue = CallContextHolder.CorrelationId.Value;
            }, LoggerFactory.VoidLogger,
                logRequest: true,
                logSubdomain: false);

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues("debug");
            contextMock.Setup(x => x.Request.Headers.TryGetValue("X-Liftr-Log-Filter-Overwrite", out val)).Returns(true);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.True(Guid.TryParse(observeredValue, out _));
        }

        [Fact]
        public async Task LivenessProbeIsAddedAsync()
        {
            string observeredValue = string.Empty;
            var middleware = new LoggingMiddleware(
                next: async (innerHttpContext) =>
                {
                    await Task.CompletedTask;
                    observeredValue = CallContextHolder.CorrelationId.Value;
                }, LoggerFactory.VoidLogger,
                logRequest: true,
                logSubdomain: false);

            var res = new MockHttpResponse();
            res.StatusCode = 404;
            var ps = new PathString("/api/liveness-probe");

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues("debug");
            contextMock.Setup(x => x.Request.Headers.TryGetValue("X-Liftr-Log-Filter-Overwrite", out val)).Returns(true);
            contextMock.Setup(x => x.Request.Path).Returns(ps);
            contextMock.Setup(x => x.Response).Returns(res);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.False(Guid.TryParse(observeredValue, out _));

            var content = res.GetContent();
            Assert.Contains("assemblyName", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("version", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LivenessProbeIsAddedFor200Async()
        {
            string observeredValue = string.Empty;
            var middleware = new LoggingMiddleware(
                next: async (innerHttpContext) =>
                {
                    await Task.CompletedTask;
                    observeredValue = CallContextHolder.CorrelationId.Value;
                }, LoggerFactory.VoidLogger,
                logRequest: true,
                logSubdomain: false);

            var res = new MockHttpResponse();
            res.StatusCode = 200;
            var ps = new PathString("/api/liveness-probe");

            var contextMock = new Mock<HttpContext>();
            StringValues val = new StringValues("debug");
            contextMock.Setup(x => x.Request.Headers.TryGetValue("X-Liftr-Log-Filter-Overwrite", out val)).Returns(true);
            contextMock.Setup(x => x.Request.Path).Returns(ps);
            contextMock.Setup(x => x.Response).Returns(res);

            await middleware.InvokeAsync(contextMock.Object);

            Assert.False(Guid.TryParse(observeredValue, out _));

            var content = res.GetContent();
            Assert.Contains("assemblyName", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("version", content, StringComparison.OrdinalIgnoreCase);
        }
    }
}
