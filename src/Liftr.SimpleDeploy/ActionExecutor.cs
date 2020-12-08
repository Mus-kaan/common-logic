//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Hosting.Contracts;
using Microsoft.Liftr.KeyVault;
using Microsoft.Liftr.Utilities;
using Microsoft.Rest.Azure;
using Serilog.Context;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.SimpleDeploy
{
    public sealed partial class ActionExecutor : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly RunnerCommandOptions _commandOptions;
        private readonly HostingOptions _hostingOptions;
        private SimpleDeployConfigurations _callBackConfigs;

        public ActionExecutor(
            Serilog.ILogger logger,
            IHostApplicationLifetime appLifetime,
            RunnerCommandOptions runnerOptions,
            HostingOptions hostingOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
            _commandOptions = runnerOptions ?? throw new ArgumentNullException(nameof(runnerOptions));
            _hostingOptions = hostingOptions ?? throw new ArgumentNullException(nameof(hostingOptions));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(_commandOptions.ConfigPath))
                {
                    var errMsg = $"Config json file doesn't exist at the path: {_commandOptions.ConfigPath}";
                    _logger.Fatal(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                var hostingEnvironmentOptions = _hostingOptions.Environments.Where(env => env.EnvironmentName.ToString().OrdinalEquals(_commandOptions.EnvName)).FirstOrDefault();

                if (hostingEnvironmentOptions == null)
                {
                    var ex = new InvalidOperationException($"Cannot find the hosting environment with name '{_commandOptions.EnvName}' in the configuration file: {_commandOptions.ConfigPath}");
                    _logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                File.WriteAllText("partner-name.txt", _hostingOptions.PartnerName);
                LogContext.PushProperty("TargetSubscriptionId", hostingEnvironmentOptions.AzureSubscription);
                File.WriteAllText("subscription-id.txt", hostingEnvironmentOptions.AzureSubscription.ToString());
                if (!string.IsNullOrEmpty(_hostingOptions.HelmReleaseName))
                {
                    File.WriteAllText("helm-releasename.txt", _hostingOptions.HelmReleaseName);
                }

                if (_hostingOptions.EnableThanos)
                {
                    File.WriteAllText("enable-thanos.txt", "true");
                }

                _logger.Information("ActionExecutor Action:{ExeAction}", _commandOptions.Action);
                _logger.Information("RunnerCommandOptions: {@RunnerCommandOptions}", _commandOptions);

                if (_commandOptions.Action == ActionType.OutputSubscriptionId)
                {
                    _appLifetime.StopApplication();
                    return;
                }

                TokenCredential tokenCredential = null;
                Func<AzureCredentials> azureCredentialsProvider = null;
                KeyVaultClient kvClient = null;
                string tenantId = null;

                if (!string.IsNullOrEmpty(_commandOptions.AuthFile))
                {
                    _logger.Information("Use auth json file to authenticate against Azure.");
                    var authContract = AuthFileContract.FromFile(_commandOptions.AuthFile);
                    kvClient = KeyVaultClientFactory.FromClientIdAndSecret(authContract.ClientId, authContract.ClientSecret);

                    if (!string.IsNullOrEmpty(authContract.ServicePrincipalObjectId))
                    {
                        _commandOptions.ExecutingSPNObjectId = authContract.ServicePrincipalObjectId;
                    }

                    tenantId = authContract.TenantId;
                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory.FromFile(_commandOptions.AuthFile);

                    TokenCredentialOptions options = new TokenCredentialOptions()
                    {
                        AuthorityHost = new Uri(authContract.ActiveDirectoryEndpointUrl),
                    };
                    tokenCredential = new ClientSecretCredential(authContract.TenantId, authContract.ClientId, authContract.ClientSecret, options);
                }
                else
                {
                    _logger.Information("Use Managed Identity to authenticate against Azure.");
                    kvClient = KeyVaultClientFactory.FromMSI();

                    // TODO: update for non-public cloud.
                    var azEnv = AzureEnvironment.AzureGlobalCloud;
                    using TenantHelper tenantHelper = new TenantHelper(new Uri(azEnv.ResourceManagerEndpoint));
                    tenantId = await tenantHelper.GetTenantIdForSubscriptionAsync(hostingEnvironmentOptions.AzureSubscription.ToString());

                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory
                    .FromMSI(new MSILoginInformation(MSIResourceType.VirtualMachine), azEnv, tenantId)
                    .WithDefaultSubscription(hostingEnvironmentOptions.AzureSubscription.ToString());
                    tokenCredential = new ManagedIdentityCredential();
                }

                LogContext.PushProperty(nameof(tenantId), tenantId);
                File.WriteAllText("tenant-id.txt", tenantId);

                if (string.IsNullOrEmpty(_commandOptions.ExecutingSPNObjectId))
                {
                    var ex = new InvalidOperationException("Must provide an object Id of the executing spn.");
                    _logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                _hostingOptions.CheckValid();

                LiftrAzureFactory azFactory = new LiftrAzureFactory(
                    _logger,
                    tenantId,
                    _commandOptions.ExecutingSPNObjectId,
                    hostingEnvironmentOptions.AzureSubscription.ToString(),
                    tokenCredential,
                    azureCredentialsProvider);

                _callBackConfigs = new SimpleDeployConfigurations()
                {
                    LiftrAzureFactory = azFactory,
                    KeyVaultClient = kvClient,
                    RunnerCommandOptions = _commandOptions,
                    HostingOptions = _hostingOptions,
                    EnvironmentOptions = hostingEnvironmentOptions,
                    Logger = _logger,
                };

                _ = RunActionAsync(hostingEnvironmentOptions, kvClient, azFactory);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Failed at running the deployment runner. Touble-shooting guide: https://aka.ms/liftr/ev2-failure");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task RunActionAsync(HostingEnvironmentOptions targetOptions, KeyVaultClient kvClient, LiftrAzureFactory azFactory)
        {
            _logger.Information("Target environment options: {@targetOptions}", targetOptions);
            File.WriteAllText("compute-type.txt", targetOptions.IsAKS ? "aks" : "vmss");

            using (var operation = _logger.StartTimedOperation(_commandOptions.Action.ToString()))
            {
                try
                {
                    operation.SetContextProperty(nameof(_hostingOptions.PartnerName), _hostingOptions.PartnerName);
                    operation.SetContextProperty("Environment", _commandOptions.EnvName);
                    operation.SetContextProperty("Location", ToSimpleName(_commandOptions.Region)); // the simple name has a geo coordinate mapping.
                    operation.SetContextProperty("ExeAction", _commandOptions.Action.ToString());

                    _logger.Information("Current correlation Id is: {correlationId}", TelemetryContext.GetOrGenerateCorrelationId());
                    _logger.Information("You can use correlation Id '{correlationId}' to query all the related ARM logs.", TelemetryContext.GetOrGenerateCorrelationId());

                    if (targetOptions.EnablePromIcM &&
                        !string.IsNullOrEmpty(_hostingOptions.IcMConnectorId) &&
                        !string.IsNullOrEmpty(_hostingOptions.IcMNotificationEmail))
                    {
                        File.WriteAllText("icm-connector-id.txt", _hostingOptions.IcMConnectorId);
                        File.WriteAllText("icm-email.txt", _hostingOptions.IcMNotificationEmail);
                    }

                    IPPoolManager ipPool = null;
                    var globalNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, targetOptions.Global.Location);
                    var globalRGName = globalNamingContext.ResourceGroupName(targetOptions.Global.BaseName);
                    File.WriteAllText("global-vault-name.txt", globalNamingContext.KeyVaultName(targetOptions.Global.BaseName));

                    if (targetOptions.IPPerRegion > 0)
                    {
                        var ipNamePrefix = globalNamingContext.GenerateCommonName(targetOptions.Global.BaseName, noRegion: true);
                        var poolRG = ipNamePrefix + "-ip-pool-rg";
                        ipPool = new IPPoolManager(poolRG, ipNamePrefix, azFactory, _logger);
                    }

                    if (_commandOptions.Action == ActionType.ExportACRInformation)
                    {
                    }
                    else if (_commandOptions.Action == ActionType.CreateOrUpdateGlobal)
                    {
                        ResourceProviderRegister register = new ResourceProviderRegister(_logger);
                        await register.RegisterGenericHostingProvidersAndFeaturesAsync(azFactory.GenerateLiftrAzure());
                        await ManageGlobalResourcesAsync(targetOptions, kvClient, azFactory);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(targetOptions.LogAnalyticsWorkspaceId))
                        {
                            targetOptions.LogAnalyticsWorkspaceId = $"/subscriptions/{azFactory.GenerateLiftrAzure().FluentClient.SubscriptionId}/resourcegroups/{globalRGName}/providers/microsoft.operationalinsights/workspaces/{globalNamingContext.LogAnalyticsName(targetOptions.Global.BaseName)}";
                        }

                        if (_commandOptions.Action == ActionType.CreateOrUpdateRegionalData)
                        {
                            await ManageDataResourcesAsync(targetOptions, kvClient, azFactory, _hostingOptions.AllowedAcisExtensions);
                        }
                        else if (_commandOptions.Action == ActionType.CreateOrUpdateRegionalCompute)
                        {
                            await ManageComputeResourcesAsync(targetOptions, kvClient, azFactory, _hostingOptions.AllowedAcisExtensions);
                        }
                        else if (_commandOptions.Action == ActionType.PrepareK8SAppDeployment)
                        {
                            await PrepareK8SDeploymentAsync(targetOptions, kvClient, azFactory);
                        }

                        await UpdateTrafficRoutingAsync(targetOptions, kvClient, azFactory);
                    }

                    {
                        var infra = new InfrastructureV2(azFactory, kvClient, _logger);
                        var acr = await infra.GetACRAsync(targetOptions.Global.BaseName, globalNamingContext);
                        _logger.Information($"Write ACR '{acr.Name}' information to disk.");

                        if (acr == null)
                        {
                            var errMsg = "Cannot find the global ACR.";
                            _logger.Fatal(errMsg);
                            throw new InvalidOperationException(errMsg);
                        }

                        File.WriteAllText("acr-name.txt", acr.Name);
                        File.WriteAllText("acr-endpoint.txt", acr.LoginServerUrl);
                    }

                    if (SimpleDeployExtension.AfterRunAsync != null)
                    {
                        using (_logger.StartTimedOperation("Run extension action"))
                        {
                            await SimpleDeployExtension.AfterRunAsync.Invoke(_callBackConfigs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is CredentialUnavailableException)
                    {
                        _logger.Fatal(ex, "If this is running inside EV2, please redeploy the release step. The EV2 ACI might be having some transient issues.");
                    }
                    else
                    {
                        _logger.Fatal(ex, "Failed at running the deployment runner. Touble-shooting guide: https://aka.ms/liftr/ev2-failure");
                    }

                    if (ex is CloudException)
                    {
                        var cloudEx = ex as CloudException;
                        _logger.Fatal(ex, "Failed with CloudException. Status code: {statusCode}, Response: {@response}, Request: {requestUri}", cloudEx.Response.StatusCode, cloudEx.Response, cloudEx.Request.RequestUri.ToString());
                    }

                    Environment.ExitCode = -1;
                    operation.FailOperation(ex.Message);
                    throw;
                }
                finally
                {
                    _appLifetime.StopApplication();
                }
            }
        }

        private async Task<IVault> GetRegionalKeyVaultAsync(HostingEnvironmentOptions targetOptions, LiftrAzureFactory azFactory)
        {
            var options = GetRegionalOptions(targetOptions);
            string rgName, kvName = null;

            if (options.ComputeRegionOptions == null)
            {
                rgName = options.RegionNamingContext.ResourceGroupName(options.RegionOptions.DataBaseName);
                kvName = options.RegionNamingContext.KeyVaultName(options.RegionOptions.DataBaseName);
            }
            else
            {
                rgName = options.ComputeRegionNamingContext.ResourceGroupName(options.ComputeRegionOptions.ComputeBaseName);
                kvName = options.ComputeRegionNamingContext.KeyVaultName(options.ComputeRegionOptions.ComputeBaseName);
            }

            var liftrAzure = azFactory.GenerateLiftrAzure();
            var targetResourceId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.KeyVault/vaults/{kvName}";
            var kv = await liftrAzure.GetKeyVaultByIdAsync(targetResourceId);

            if (targetOptions.EnableVNet)
            {
                var currentPublicIP = await MetadataHelper.GetPublicIPAddressAsync();
                _logger.Information("Restrict VNet access to public IP: {currentPublicIP}", currentPublicIP);
                await liftrAzure.WithKeyVaultAccessFromNetworkAsync(kv, currentPublicIP, null);
            }

            return kv;
        }

        private ActionExcutorRegionOptions GetRegionalOptions(HostingEnvironmentOptions targetOptions)
        {
            var options = new ActionExcutorRegionOptions();
            var location = Region.Create(_commandOptions.Region);

            if (targetOptions.Regions.First().IsSeparatedDataAndComputeRegion &&
                (_commandOptions.Action == ActionType.CreateOrUpdateRegionalCompute ||
                _commandOptions.Action == ActionType.PrepareK8SAppDeployment ||
                _commandOptions.Action == ActionType.UpdateComputeIPInTrafficManager))
            {
                options.RegionOptions = targetOptions.Regions.FirstOrDefault(r => r.ComputeRegions.Any(computeRegion => computeRegion.Location.Name.OrdinalEquals(location.Name)));
                options.ComputeRegionOptions = targetOptions.Regions.SelectMany(r => r.ComputeRegions).FirstOrDefault(r => r.Location.Name.OrdinalEquals(location.Name));
                options.ComputeRegionNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, options.ComputeRegionOptions.Location);
            }
            else
            {
                options.RegionOptions = targetOptions.Regions.FirstOrDefault(r => r.Location.Name.OrdinalEquals(location.Name));
            }

            if (options.RegionOptions == null)
            {
                var ex = new InvalidOperationException($"Cannot find the '{_commandOptions.Region}' region configurations.");
                _logger.Fatal(ex, ex.Message);
                throw ex;
            }

            options.RegionNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, options.RegionOptions.Location);

            if (options.ComputeRegionNamingContext == null)
            {
                options.AKSRGName = options.RegionNamingContext.ResourceGroupName(options.RegionOptions.ComputeBaseName);
                options.AKSName = options.RegionNamingContext.AKSName(options.RegionOptions.ComputeBaseName);
            }
            else
            {
                options.AKSRGName = options.ComputeRegionNamingContext.ResourceGroupName(options.ComputeRegionOptions.ComputeBaseName);
                options.AKSName = options.ComputeRegionNamingContext.AKSName(options.ComputeRegionOptions.ComputeBaseName);
            }

            return options;
        }

        private async Task<IPublicIPAddress> WriteReservedIPToDiskAsync(
            LiftrAzureFactory azFactory,
            string aksRGName,
            string aksName,
            Region aksLocation,
            HostingEnvironmentOptions targetOptions,
            IPPoolManager ipPool)
        {
            if (targetOptions.IPPerRegion == 0)
            {
                return null;
            }

            var aksHelper = new AKSNetworkHelper(_logger);
            var pip = await aksHelper.GetAKSPublicIPAsync(azFactory.GenerateLiftrAzure(), aksRGName, aksName, aksLocation);
            if (pip == null)
            {
                pip = await ipPool.GetAvailableIPAsync(aksLocation);
            }

            if (pip != null)
            {
                File.WriteAllText("public-ip.txt", pip.IPAddress);
                File.WriteAllText("public-ip-rg.txt", pip.ResourceGroupName);
            }
            else
            {
                var ex = new InvalidOperationException("There is no available IP address for the AKS cluster to use. Please clean up old ones first.");
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            return pip;
        }

        private static string ToSimpleName(string region)
        {
            return region.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
        }
    }
}
