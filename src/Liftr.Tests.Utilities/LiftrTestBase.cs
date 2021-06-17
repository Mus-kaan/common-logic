//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Tests.Utilities;
using Microsoft.Liftr.Tests.Utilities.Trait;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
        private const string LIFTR_UNIT_TEST_AGGREGATOR = nameof(LIFTR_UNIT_TEST_AGGREGATOR);
        private const string LIFTR_UNIT_TEST_COMPONENT_TAG = nameof(LIFTR_UNIT_TEST_COMPONENT_TAG);
        private static readonly IDisposable s_httpClientSubscriber = GetHttpCoreDiagnosticSourceSubscriber();
        private static readonly string s_appInsightsIntrumentationKey = GetInstrumentationKey();

        private TelemetryConfiguration _appInsightsConfig;
        private DependencyTrackingTelemetryModule _depModule;
        private TelemetryClient _appInsightsClient;
        private bool _disposed = false;

        static LiftrTestBase()
        {
            TestExceptionHelper.EnableExceptionCapture();
        }

        public LiftrTestBase(ITestOutputHelper output, bool useMethodName = true, [CallerFilePath] string sourceFile = "")
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            TestExceptionHelper.Register(sourceFile);
            var testClass = GetType().Name;
            GenerateLogger(testClass, output);
            string operationName = null;
            DateTimeStr = DateTime.UtcNow.ToString("MMddHmmss", CultureInfo.InvariantCulture);

            try
            {
                var testOutputType = output.GetType();
                FieldInfo cachedTestMember = testOutputType.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
                if (cachedTestMember != null)
                {
                    Test = (ITest)cachedTestMember.GetValue(output);

                    var traits = Test?.TestCase?.Traits;

                    if (traits != null)
                    {
                        if (traits.ContainsKey(nameof(CloudType)))
                        {
                            if (Enum.TryParse<CloudType>(traits[nameof(CloudType)].Last(), out var cloudType))
                            {
                                TestCloudType = cloudType;
                            }
                        }

                        if (traits.ContainsKey(nameof(AzureRegion)))
                        {
                            TestAzureRegion = new AzureRegion(traits[nameof(AzureRegion)].Last());
                        }

                        if (traits.ContainsKey(TraitConstants.RegionCategory))
                        {
                            TestRegionCategory = traits[TraitConstants.RegionCategory].Last();
                        }
                    }

                    var parts = Test.DisplayName.Split('.');
                    TestClassName = parts[parts.Length - 2];
                    TestMethodName = parts[parts.Length - 1];

                    operationName = useMethodName ? $"{TestClassName}-{TestMethodName}" : TestClassName;

                    if (TestCloudType != null && TestAzureRegion != null)
                    {
                        operationName = $"{operationName}-{TestCloudType}-{TestAzureRegion.Name}";
                    }
                }
            }
            catch
            {
                operationName = testClass;
            }

            TimedOperation = Logger.StartTimedOperation(operationName, generateMetrics: true, generatePrometheus: false);
            TimedOperation.SetProperty("TestEnv", "CICD");

            if (!string.IsNullOrEmpty(TestClassName))
            {
                TimedOperation.SetProperty(nameof(TestClassName), TestClassName);
            }

            if (!string.IsNullOrEmpty(TestMethodName))
            {
                TimedOperation.SetProperty(nameof(TestMethodName), TestMethodName);
            }

            if (TestCloudType != null && TestAzureRegion != null)
            {
                TimedOperation.SetContextProperty(nameof(TestCloudType), TestCloudType.ToString());
                TimedOperation.SetContextProperty(nameof(TestAzureRegion), TestAzureRegion.Name);
            }
        }

        public string TestClassName { get; }

        public string TestMethodName { get; }

        public string DateTimeStr { get; }

        public CloudType? TestCloudType { get; }

        public AzureRegion TestAzureRegion { get; }

        public string TestRegionCategory { get; }

        public ITest Test { get; }

        public ILogger Logger { get; private set; }

        public ITimedOperation TimedOperation { get; private set; }

        public bool? IsFailure { get; private set; }

        protected Action OnTestFailure { get; set; }

        public virtual void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                var theExceptionThrownByTest = TestExceptionHelper.TestException;
                if (theExceptionThrownByTest != null)
                {
                    IsFailure = true;
                    if (TimedOperation != null)
                    {
                        Logger.Error(theExceptionThrownByTest, $"test_failure. {TimedOperation.Name}");
                        TimedOperation.FailOperation(theExceptionThrownByTest.Message);
                    }

                    if (OnTestFailure != null)
                    {
                        try
                        {
                            OnTestFailure.Invoke();
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    IsFailure = false;
                }

                TimedOperation?.Dispose();

                try
                {
                    var aggregatorEndpoint = Environment.GetEnvironmentVariable(LIFTR_UNIT_TEST_AGGREGATOR);
                    if (!string.IsNullOrEmpty(aggregatorEndpoint))
                    {
                        var testComponent = Environment.GetEnvironmentVariable(LIFTR_UNIT_TEST_COMPONENT_TAG);
                        var testResultMetirc = new TestResult()
                        {
                            OperationName = TimedOperation.Name,
                            HelpText = $"Test run metric for {TestClassName}.{TestMethodName}",
                            TestClass = TestClassName,
                            TestMethod = TestMethodName,
                            IsFailure = !TimedOperation.IsSuccessful,
                            DurationMilliseconds = TimedOperation.ElapsedMilliseconds,
                        };

                        if (!string.IsNullOrEmpty(testComponent))
                        {
                            testResultMetirc.Component = testComponent;
                        }

                        if (TestCloudType != null)
                        {
                            testResultMetirc.TestCloudType = TestCloudType.ToString();
                            testResultMetirc.TestAzureRegion = TestAzureRegion.Name;
                            testResultMetirc.TestRegionCategory = TestRegionCategory;
                        }

                        var httpWebRequest = WebRequest.CreateHttp(aggregatorEndpoint);
                        httpWebRequest.ContentType = "application/json; charset=utf-8";
                        httpWebRequest.Method = "POST";

                        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                        {
                            streamWriter.Write(testResultMetirc.ToJson());
                            streamWriter.Flush();
                        }

                        httpWebRequest.GetResponse();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed at pusing metrics");
                }

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
            var builder = _appInsightsConfig.TelemetryProcessorChainBuilder;
            builder.Use((next) => new LocalHostTelemetryFilter(next));
            builder.Build();

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
