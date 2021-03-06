//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Logging;
using Serilog;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Liftr
{
    public class TestResourceGroupScope : IDisposable
    {
        private static readonly IDisposable s_httpClientSubscriber = GetHttpCoreDiagnosticSourceSubscriber();
        private static readonly string s_appInsightsIntrumentationKey = GetInstrumentationKey();

        private TelemetryConfiguration _appInsightsConfig;
        private DependencyTrackingTelemetryModule _depModule;
        private TelemetryClient _appInsightsClient;

        public TestResourceGroupScope(string resourceGroupName, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            GenerateLogger(filePath, memberName);
            AzFactory = new LiftrAzureFactory(Logger, TestCredentials.TenantId, TestCredentials.ObjectId, TestCredentials.SubscriptionId, TestCredentials.TokenCredential, TestCredentials.GetAzureCredentials);
            ResourceGroupName = resourceGroupName;

            var operationName = $"{Path.GetFileNameWithoutExtension(filePath)}-{memberName}";
            TimedOperation = Logger.StartTimedOperation(operationName, generateMetrics: true);
            TimedOperation.SetContextProperty(nameof(ResourceGroupName), ResourceGroupName);
            TimedOperation.SetProperty("TestEnv", "CICD");
        }

        public TestResourceGroupScope(string baseName, ITestOutputHelper output, EnvironmentType? env = null, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", bool loadCredentials = true)
        {
            GenerateLogger(filePath, memberName, output);
            if (loadCredentials)
            {
                AzFactory = new LiftrAzureFactory(Logger, TestCredentials.TenantId, TestCredentials.ObjectId, TestCredentials.SubscriptionId, TestCredentials.TokenCredential, TestCredentials.GetAzureCredentials);
            }

            ResourceGroupName = SdkContext.RandomResourceName(baseName, 25);

            var operationName = $"{Path.GetFileNameWithoutExtension(filePath)}-{memberName}";
            TimedOperation = Logger.StartTimedOperation(operationName, generateMetrics: true);
            TimedOperation.SetContextProperty(nameof(ResourceGroupName), ResourceGroupName);
            if (env != null)
            {
                TimedOperation.SetContextProperty(nameof(EnvironmentType), env.ToString());
                TimedOperation.SetEnvironmentType(env.ToString());
            }

            TimedOperation.SetProperty("TestEnv", "CICD");
        }

        public TestResourceGroupScope(string baseName, NamingContext namingContext, ITestOutputHelper output, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            GenerateLogger(filePath, memberName, output);
            AzFactory = new LiftrAzureFactory(Logger, TestCredentials.TenantId, TestCredentials.ObjectId, TestCredentials.SubscriptionId, TestCredentials.TokenCredential, TestCredentials.GetAzureCredentials);
            ResourceGroupName = namingContext.ResourceGroupName(baseName);
            TestCommon.AddCommonTags(namingContext.Tags);

            var operationName = $"{Path.GetFileNameWithoutExtension(filePath)}-{memberName}";
            TimedOperation = Logger.StartTimedOperation(operationName, generateMetrics: true);
            TimedOperation.SetContextProperty(nameof(ResourceGroupName), ResourceGroupName);
            TimedOperation.SetProperty("TestEnv", "CICD");
        }

        public ILogger Logger { get; private set; }

        public ITimedOperation TimedOperation { get; private set; }

        public bool SkipDeleteResourceGroup { get; set; } = false;

        public LiftrAzureFactory AzFactory { get; protected set; }

        public ILiftrAzure Client
        {
            get
            {
                return AzFactory.GenerateLiftrAzure();
            }
        }

        public NamingContext Naming { get; }

        public string ResourceGroupName { get; }

        public async Task<IStorageAccount> GetTestStorageAccountAsync()
        {
            var storageAccountName = SdkContext.RandomResourceName("ut", 15);
            var az = AzFactory.GenerateLiftrAzure();
            await az.GetOrCreateResourceGroupAsync(TestCommon.Location, ResourceGroupName, TestCommon.Tags);
            return await az.GetOrCreateStorageAccountAsync(TestCommon.Location, ResourceGroupName, storageAccountName, TestCommon.Tags);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        protected virtual void Dispose(bool bothManagedAndNative)
        {
            if (!bothManagedAndNative)
            {
                return;
            }

            try
            {
                TimedOperation?.Dispose();
                _appInsightsClient?.Flush();
                _appInsightsConfig?.Dispose();
                _depModule?.Dispose();
                if (!SkipDeleteResourceGroup)
                {
                    var deleteTask = Client.DeleteResourceGroupAsync(ResourceGroupName);
                    Task.Yield();
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
                    Task.Delay(2000).Wait();
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
                }

                TimedOperation = null;
                _appInsightsClient = null;
                _appInsightsConfig = null;
                _depModule = null;
            }
            catch
            {
            }
        }

        private void GenerateLogger(string filePath, string memberName, ITestOutputHelper output = null)
        {
            _appInsightsConfig = new TelemetryConfiguration(s_appInsightsIntrumentationKey);
            var builder = _appInsightsConfig.TelemetryProcessorChainBuilder;
            builder.Use((next) => new LocalHostTelemetryFilter(next));
            builder.Build();

            _depModule = new DependencyTrackingTelemetryModule();
            _depModule.Initialize(_appInsightsConfig);
            _appInsightsClient = new TelemetryClient(_appInsightsConfig);
            AppInsightsHelper.AppInsightsClient = _appInsightsClient;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var loggerConfig = new LoggerConfiguration()
                .Enrich.WithProperty("TestClassName", fileName)
                .Enrich.WithProperty("TestFunctionName", memberName)
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
