﻿//-----------------------------------------------------------------------------
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
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;
using Xunit.Sdk;

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

        public LiftrTestBase(ITestOutputHelper output, [CallerFilePath] string sourceFile = "")
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            TestExceptionHelper.Register(sourceFile);
            var testClass = GetType().Name;
            GenerateLogger(testClass, output);
            string operationName = null;

            try
            {
                var testOutputType = output.GetType();
                FieldInfo cachedTestMember = testOutputType.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
                if (cachedTestMember != null)
                {
                    Test = (ITest)cachedTestMember.GetValue(output);
                    var parts = Test.DisplayName.Split('.');
                    TestClassName = parts[parts.Length - 2];
                    TestMethodName = parts[parts.Length - 1];
                    operationName = $"{TestClassName}-{TestClassName}";
                }
            }
            catch
            {
                operationName = testClass;
            }

            TimedOperation = Logger.StartTimedOperation(operationName, generateMetrics: true);
            TimedOperation.SetProperty("TestEnv", "CICD");

            if (!string.IsNullOrEmpty(TestClassName))
            {
                TimedOperation.SetProperty(nameof(TestClassName), TestClassName);
            }

            if (!string.IsNullOrEmpty(TestMethodName))
            {
                TimedOperation.SetProperty(nameof(TestMethodName), TestMethodName);
            }
        }

        public string TestClassName { get; }

        public string TestMethodName { get; }

        public ITest Test { get; }

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
                        Logger.Error(theExceptionThrownByTest, $"test_failure. {TimedOperation.Name}");
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

            var logToConsole = Environment.GetEnvironmentVariable("LIFTR_TEST_CONSOLE_LOG");
            if (logToConsole.OrdinalEquals("true"))
            {
                loggerConfig = loggerConfig.WriteTo.Console();
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
