//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Tests.Utilities;
using Serilog;
using System;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Tests
{
    /// <summary>
    /// Add and expose common test functionalities.
    /// xUnit does not provide native TestContext. https://github.com/xunit/xunit/issues/621
    /// This added the test context through 'XunitContext'. https://github.com/SimonCropp/XunitContext#test-failure
    /// </summary>
    public class LiftrTestBase : IDisposable
    {
        private static readonly string s_appInsightsIntrumentationKey = GetInstrumentationKey();
        private static readonly IDisposable s_httpClientSubscriber = GetHttpCoreDiagnosticSourceSubscriber();

        private TelemetryConfiguration _appInsightsConfig;
        private DependencyTrackingTelemetryModule _depModule;
        private TelemetryClient _appInsightsClient;

        static LiftrTestBase()
        {
            TestExceptionHelper.EnableExceptionCapture();
        }

        public LiftrTestBase(ITestOutputHelper output = null, [CallerFilePath] string sourceFile = "")
        {
            TestExceptionHelper.Register(sourceFile);
            var testClass = GetType().Name;
            GenerateLogger(testClass, output);
            TimedOperation = Logger.StartTimedOperation(testClass, generateMetrics: true);
            TimedOperation.SetProperty("TestEnv", "CICD");
        }

        public ILogger Logger { get; private set; }

        public ITimedOperation TimedOperation { get; private set; }

        protected Action OnTestFailure { get; set; }

        public virtual void Dispose()
        {
            try
            {
                var theExceptionThrownByTest = TestExceptionHelper.TestException;
                if (theExceptionThrownByTest != null)
                {
                    if (TimedOperation != null)
                    {
                        TimedOperation.FailOperation(theExceptionThrownByTest.Message);
                    }

                    if (OnTestFailure != null)
                    {
                        OnTestFailure.Invoke();
                    }
                }

                TimedOperation?.Dispose();
                _appInsightsClient?.Flush();
                _appInsightsConfig?.Dispose();
                _depModule?.Dispose();
                TimedOperation = null;
                _appInsightsClient = null;
                _appInsightsConfig = null;
                _depModule = null;
            }
            catch
            {
            }
        }

        private void GenerateLogger(string testClass, ITestOutputHelper output = null)
        {
            _appInsightsConfig = new TelemetryConfiguration(s_appInsightsIntrumentationKey);
            _depModule = new DependencyTrackingTelemetryModule();
            _depModule.Initialize(_appInsightsConfig);
            _appInsightsClient = new TelemetryClient(_appInsightsConfig);
            AppInsightsHelper.AppInsightsClient = _appInsightsClient;

            var loggerConfig = new LoggerConfiguration()
                .Enrich.WithProperty("TestClassName", testClass)
                .Enrich.WithProperty("UnitTestessionId", Guid.NewGuid().ToString())
                .Enrich.WithProperty("UnitTestStartTime", DateTime.UtcNow.ToZuluString())
                .WriteTo.ApplicationInsights(_appInsightsClient, TelemetryConverter.Events);

            if (output != null)
            {
                loggerConfig = loggerConfig.WriteTo.Xunit(output);
            }

            Logger = loggerConfig.Enrich.FromLogContext().CreateLogger();
        }

        private static string GetInstrumentationKey()
        {
            if (s_httpClientSubscriber == null)
            {
                Console.WriteLine($"'{nameof(s_httpClientSubscriber)}' is null.");
            }

            var ikey = Environment.GetEnvironmentVariable("LIFTR_APPINSIGHTS_IKEY");
            if (string.IsNullOrEmpty(ikey))
            {
                // /subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourcegroups/liftr-dev-wus-rg/providers/microsoft.insights/components/liftr-unittest-wus2-ai
                ikey = "78b3bb82-b6b7-42bf-93d8-c8ba1ca26331";
            }

            return ikey;
        }

        private static IDisposable GetHttpCoreDiagnosticSourceSubscriber()
        {
            return new HttpCoreDiagnosticSourceSubscriber(new HttpCoreDiagnosticSourceListener());
        }
    }
}
