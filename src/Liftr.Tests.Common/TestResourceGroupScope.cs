//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Serilog;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Liftr
{
    public sealed class TestResourceGroupScope : IDisposable
    {
        private TelemetryConfiguration _appInsightsConfig;
        private TelemetryClient _appInsightsClient;

        public TestResourceGroupScope(string resourceGroupName, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            GenerateLogger(filePath, memberName);
            AzFactory = new LiftrAzureFactory(Logger, TestCredentials.SubscriptionId, TestCredentials.GetAzureCredentials);
            ResourceGroupName = resourceGroupName;
        }

        public TestResourceGroupScope(string baseName, ITestOutputHelper output, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            GenerateLogger(filePath, memberName, output);
            AzFactory = new LiftrAzureFactory(Logger, TestCredentials.SubscriptionId, TestCredentials.GetAzureCredentials);
            ResourceGroupName = SdkContext.RandomResourceName(baseName, 25);
        }

        public TestResourceGroupScope(string baseName, NamingContext namingContext, ITestOutputHelper output, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            GenerateLogger(filePath, memberName, output);
            AzFactory = new LiftrAzureFactory(Logger, TestCredentials.SubscriptionId, TestCredentials.GetAzureCredentials);
            ResourceGroupName = namingContext.ResourceGroupName(baseName);
            TestCommon.AddCommonTags(namingContext.Tags);
        }

        public Serilog.ILogger Logger { get; private set; }

        public LiftrAzureFactory AzFactory { get; }

        public ILiftrAzure Client
        {
            get
            {
                return AzFactory.GenerateLiftrAzure();
            }
        }

        public NamingContext Naming { get; }

        public string ResourceGroupName { get; }

        public async Task<CloudStorageAccount> GetTestStorageAccountAsync()
        {
            var name = SdkContext.RandomResourceName("ut", 15);
            var az = AzFactory.GenerateLiftrAzure();
            var rg = await az.GetOrCreateResourceGroupAsync(TestCommon.Location, ResourceGroupName, TestCommon.Tags);
            var stor = await az.GetOrCreateStorageAccountAsync(TestCommon.Location, ResourceGroupName, name, TestCommon.Tags);
            var keys = await stor.GetKeysAsync();
            var key = keys[0];
            var cred = new StorageCredentials(name, key.Value, key.KeyName);
            return new CloudStorageAccount(cred, useHttps: true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public void Dispose()
        {
            try
            {
                _appInsightsClient?.Flush();
                _appInsightsConfig?.Dispose();
                var deleteTask = Client.DeleteResourceGroupAsync(ResourceGroupName);
                Task.Yield();
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
                Task.Delay(2000).Wait();
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
            }
            catch
            {
            }
        }

        private void GenerateLogger(string filePath, string memberName, ITestOutputHelper output = null)
        {
            // /subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourcegroups/liftr-dev-wus-rg/providers/microsoft.insights/components/liftr-unittest-wus2-ai
            _appInsightsConfig = new TelemetryConfiguration("78b3bb82-b6b7-42bf-93d8-c8ba1ca26331");
            _appInsightsClient = new TelemetryClient(_appInsightsConfig);

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
    }
}
