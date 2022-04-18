//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.EV2.Contracts;
using Microsoft.Liftr.Hosting.Contracts;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Liftr.EV2
{
    public class EV2ArtifactsGenerator
    {
        private readonly Serilog.ILogger _logger;

        public EV2ArtifactsGenerator(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public void GenerateArtifacts(EV2HostingOptions ev2Options, HostingOptions hostingOptions, string outputDirectory)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            if (hostingOptions == null)
            {
                throw new ArgumentNullException(nameof(hostingOptions));
            }

            LogContext.PushProperty(nameof(hostingOptions.PartnerName), hostingOptions.PartnerName);
            LogContext.PushProperty(nameof(ev2Options.ServiceTreeName), ev2Options.ServiceTreeName);
            LogContext.PushProperty(nameof(ev2Options.NotificationEmail), ev2Options.NotificationEmail);
            LogContext.PushProperty(nameof(ev2Options.ServiceTreeId), ev2Options.ServiceTreeId.ToString());

            _logger.Information("----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------");
            _logger.Information("Thanks for using Liftr generic hosting! More details: https://aka.ms/liftr/hosting");
            _logger.Information("==========[Liftr]==========[https://aka.ms/liftr]==========[Liftr]==========[https://aka.ms/liftr]==========");

            try
            {
                ev2Options.CheckValid();

                Directory.CreateDirectory(outputDirectory);

                GenerateImportAppImgArtifacts(ev2Options, hostingOptions, Path.Combine(outputDirectory, ArtifactConstants.c_AppImgFolderName));
                GenerateGlobalArtifacts(ev2Options, hostingOptions, Path.Combine(outputDirectory, ArtifactConstants.c_GlobalFolderName));
                GenerateRegionDataArtifacts(ev2Options, hostingOptions, Path.Combine(outputDirectory, ArtifactConstants.c_RegionalDataFolderName));
                GenerateRegionComputeArtifacts(ev2Options, hostingOptions, Path.Combine(outputDirectory, ArtifactConstants.c_RegionalComputeFolderName));
                GenerateAKSAppArtifacts(ev2Options, hostingOptions, Path.Combine(outputDirectory, ArtifactConstants.c_ApplicationFolderName));
                GenerateTMArtifacts(ev2Options, hostingOptions, Path.Combine(outputDirectory, ArtifactConstants.c_TMFolderName));

                _logger.Information("Generated EV2 rollout artifacts and stored in directory: {outputDirectory}", outputDirectory);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw;
            }
        }

        public void GenerateImageBuilderArtifacts(EV2ImageBuilderOptions imageBuilderOptions, string outputDirectory)
        {
            if (imageBuilderOptions == null)
            {
                throw new ArgumentNullException(nameof(imageBuilderOptions));
            }

            LogContext.PushProperty(nameof(imageBuilderOptions.ServiceTreeName), imageBuilderOptions.ServiceTreeName);
            LogContext.PushProperty(nameof(imageBuilderOptions.NotificationEmail), imageBuilderOptions.NotificationEmail);
            LogContext.PushProperty(nameof(imageBuilderOptions.ServiceTreeId), imageBuilderOptions.ServiceTreeId.ToString());

            _logger.Information("----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------");
            _logger.Information("Thanks for using Liftr VM image builder! More details: https://aka.ms/liftr/img");
            _logger.Information("==========[Liftr]==========[https://aka.ms/liftr]==========[Liftr]==========[https://aka.ms/liftr]==========");

            try
            {
                imageBuilderOptions.CheckValid();

                outputDirectory = Path.Combine(outputDirectory, ArtifactConstants.c_ImageBuilderFolderName);
                Directory.CreateDirectory(outputDirectory);

                var regions = new List<string>() { "Global" };

                foreach (var image in imageBuilderOptions.Images)
                {
                    BakeImageArtifacts(imageBuilderOptions, image, outputDirectory);

                    if (image.Distribute != null && image.Distribute.Any())
                    {
                        int i = 1;
                        foreach (var dist in image.Distribute)
                        {
                            DistributeImageArtifacts(imageBuilderOptions, image, dist, outputDirectory, i++);
                        }
                    }
                }

                _logger.Information("Generated EV2 image builder artifacts and stored in directory: {outputDirectory}", outputDirectory);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw;
            }
        }

        private static void BakeImageArtifacts(EV2ImageBuilderOptions imageBuilderOptions, ImageOptions image, string outputDirectory)
        {
            var fileBaseName = string.IsNullOrEmpty(image.Bake.Name) ? $"{image.ImageName}-bake" : image.Bake.Name;
            var regions = new List<string>() { "Global" };

            var scopeBindings = AssembleScopeBindings();
            File.WriteAllText(Path.Combine(outputDirectory, ArtifactConstants.c_ScopeBindingsFileName), scopeBindings.ToJsonString(indented: true));

            var serviceModel = AssembleServiceModel(
                        "Production", // hard code the env name for image builder artifacts.
                        regions,
                        imageBuilderOptions.ServiceTreeName,
                        imageBuilderOptions.ServiceTreeId,
                        image.Bake.RunnerInformation.Location,
                        image.Bake.RunnerInformation.SubscriptionId,
                        (_, region) => ArtifactConstants.RolloutParametersFileName(fileBaseName));

            File.WriteAllText(Path.Combine(outputDirectory, ArtifactConstants.ServiceModelFileName(fileBaseName)), serviceModel.ToJsonString(indented: true));

            var rollputSpec = AssembleRolloutSpec(
                fileBaseName,
                "global",
                description: $"[{image.ImageName}]{GetCDPxVersionText()} Bake VM image",
                imageBuilderOptions.NotificationEmail);
            File.WriteAllText(Path.Combine(outputDirectory, ArtifactConstants.RolloutSpecFileName(fileBaseName)), rollputSpec.ToJsonString(indented: true));

            var parameterFileName = ArtifactConstants.RolloutParametersFileName(fileBaseName);
            var parameterFilePath = Path.Combine(outputDirectory, parameterFileName);
            var parameters = AssembleImageBuilderRolloutParameters(
                image,
                image.Bake.ConfigurationPath,
                image.Bake.RunnerInformation.UserAssignedManagedIdentityResourceId,
                image.Bake.RunnerInformation.UserAssignedManagedIdentityObjectId,
                image.Bake.Cloud,
                entryScript: "1_BakeVMImage.sh",
                oneBranchContainerImages: image.OneBranchContainerImages);
            File.WriteAllText(parameterFilePath, parameters.ToJsonString(indented: true));
        }

        private static void DistributeImageArtifacts(EV2ImageBuilderOptions imageBuilderOptions, ImageOptions image, EnvironmentOptions distribute, string outputDirectory, int num)
        {
            var fileBaseName = string.IsNullOrEmpty(distribute.Name) ? $"{image.ImageName}-dist{num}-{distribute.Cloud}" : distribute.Name;
            var regions = new List<string>() { "Global" };

            var scopeBindings = AssembleScopeBindings();
            File.WriteAllText(Path.Combine(outputDirectory, ArtifactConstants.c_ScopeBindingsFileName), scopeBindings.ToJsonString(indented: true));

            var serviceModel = AssembleServiceModel(
                        "Production", // hard code the env name for image builder artifacts.
                        regions,
                        imageBuilderOptions.ServiceTreeName,
                        imageBuilderOptions.ServiceTreeId,
                        distribute.RunnerInformation.Location,
                        distribute.RunnerInformation.SubscriptionId,
                        (_, region) => ArtifactConstants.RolloutParametersFileName(fileBaseName));
            File.WriteAllText(Path.Combine(outputDirectory, ArtifactConstants.ServiceModelFileName(fileBaseName)), serviceModel.ToJsonString(indented: true));

            var rollputSpec = AssembleRolloutSpec(
                fileBaseName,
                "global",
                description: $"[{image.ImageName}]{GetCDPxVersionText()} Import VM image",
                imageBuilderOptions.NotificationEmail);
            File.WriteAllText(Path.Combine(outputDirectory, ArtifactConstants.RolloutSpecFileName(fileBaseName)), rollputSpec.ToJsonString(indented: true));

            var parameterFileName = ArtifactConstants.RolloutParametersFileName(fileBaseName);
            var parameterFilePath = Path.Combine(outputDirectory, parameterFileName);
            var parameters = AssembleImageBuilderRolloutParameters(
                image,
                distribute.ConfigurationPath,
                distribute.RunnerInformation.UserAssignedManagedIdentityResourceId,
                distribute.RunnerInformation.UserAssignedManagedIdentityObjectId,
                distribute.Cloud,
                entryScript: "2_ImportVMImage.sh",
                oneBranchContainerImages: null);
            File.WriteAllText(parameterFilePath, parameters.ToJsonString(indented: true));
        }

        private static void GenerateImportAppImgArtifacts(EV2HostingOptions ev2Options, HostingOptions hostingOptions, string outputDirectory)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            Directory.CreateDirectory(outputDirectory);

            // The regions will be overwritten as 'global'.
            // We need the original regions for generating regional artifacts.
            // Clone the instance then make the change.
            ev2Options = ev2Options.Clone();
            var regions = new List<string>() { "Global" };

            foreach (var targetEnvironment in ev2Options.TargetEnvironments)
            {
                GenerateEnvironmentArtifacts(
                    ev2Options,
                    targetEnvironment,
                    regions,
                    outputDirectory,
                    description: "Import Application Images",
                    entryScript: "0_ImportAppImage.sh",
                    uploadImageToACR: true);
            }
        }

        private static void GenerateGlobalArtifacts(EV2HostingOptions ev2Options, HostingOptions hostingOptions, string outputDirectory)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            Directory.CreateDirectory(outputDirectory);

            // The regions will be overwritten as 'global'.
            // We need the original regions for generating regional artifacts.
            // Clone the instance then make the change.
            ev2Options = ev2Options.Clone();
            var regions = new List<string>() { "Global" };

            foreach (var targetEnvironment in ev2Options.TargetEnvironments)
            {
                GenerateEnvironmentArtifacts(
                    ev2Options,
                    targetEnvironment,
                    regions,
                    outputDirectory,
                    description: "Global resources",
                    entryScript: "1_ManageGlobalResources.sh");
            }
        }

        private static void GenerateRegionDataArtifacts(EV2HostingOptions ev2Options, HostingOptions hostingOptions, string outputDirectory)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            foreach (var targetEnvironment in ev2Options.TargetEnvironments)
            {
                var regions = ParseRegions(targetEnvironment, hostingOptions);

                GenerateEnvironmentArtifacts(
                    ev2Options,
                    targetEnvironment,
                    regions,
                    outputDirectory,
                    description: "Regional data resources",
                    entryScript: "2_ManageRegionalData.sh");
            }
        }

        private static void GenerateRegionComputeArtifacts(EV2HostingOptions ev2Options, HostingOptions hostingOptions, string outputDirectory)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            foreach (var targetEnvironment in ev2Options.TargetEnvironments)
            {
                var regions = ParseRegions(targetEnvironment, hostingOptions);

                GenerateEnvironmentArtifacts(
                    ev2Options,
                    targetEnvironment,
                    regions,
                    outputDirectory,
                    description: "Regional compute resources (AKS, Geneva, Pod identity, Nginx Ingress)",
                    entryScript: "3_ManageRegionalCompute.sh");
            }
        }

        private static void GenerateAKSAppArtifacts(EV2HostingOptions ev2Options, HostingOptions hostingOptions, string outputDirectory)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            foreach (var targetEnvironment in ev2Options.TargetEnvironments)
            {
                var regions = ParseRegions(targetEnvironment, hostingOptions);

                GenerateEnvironmentArtifacts(
                    ev2Options,
                    targetEnvironment,
                    regions,
                    outputDirectory,
                    description: "Deploy AKS applications",
                    entryScript: "4_DeployAKSApp.sh",
                    uploadImageToACR: true);
            }
        }

        private static void GenerateTMArtifacts(EV2HostingOptions ev2Options, HostingOptions hostingOptions, string outputDirectory)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            foreach (var targetEnvironment in ev2Options.TargetEnvironments)
            {
                var regions = ParseRegions(targetEnvironment, hostingOptions);

                GenerateEnvironmentArtifacts(
                    ev2Options,
                    targetEnvironment,
                    regions,
                    outputDirectory,
                    description: "Update the AKS Public IP in Traffic Manager",
                    entryScript: "5_UpdateTrafficManager.sh");
            }
        }

        private static IEnumerable<string> ParseRegions(TargetEnvironment targetEnvironment, HostingOptions hostingOptions)
        {
            var hostEnv = hostingOptions.Environments.FirstOrDefault(e => e.EnvironmentName == targetEnvironment.EnvironmentName);
            if (hostEnv == null)
            {
                throw new InvalidOperationException($"Cannot find environment '{targetEnvironment.EnvironmentName}' in hosting options.");
            }

            var regions = hostEnv.Regions.Select(r => r.Location.Name);

            return regions;
        }

        private static void GenerateEnvironmentArtifacts(
            EV2HostingOptions ev2Options,
            TargetEnvironment targetEnvironment,
            IEnumerable<string> regions,
            string outputDirectory,
            string description,
            string entryScript,
            bool uploadImageToACR = false)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            if (targetEnvironment == null)
            {
                throw new ArgumentNullException(nameof(targetEnvironment));
            }

            Directory.CreateDirectory(outputDirectory);

            var scopeBindings = AssembleScopeBindings();
            File.WriteAllText(Path.Combine(outputDirectory, ArtifactConstants.c_ScopeBindingsFileName), scopeBindings.ToJsonString(indented: true));

            var envName = targetEnvironment.EnvironmentName.ToString();

            var serviceModel = AssembleServiceModel(
                envName,
                regions,
                ev2Options.ServiceTreeName,
                ev2Options.ServiceTreeId,
                targetEnvironment.RunnerInformation.Location,
                targetEnvironment.RunnerInformation.SubscriptionId,
                ArtifactConstants.RolloutParametersPath);
            File.WriteAllText(Path.Combine(outputDirectory, ArtifactConstants.ServiceModelFileName(targetEnvironment.EnvironmentName)), serviceModel.ToJsonString(indented: true));

            foreach (var region in regions)
            {
                var simplifiedRegion = ToSimpleName(region);
                var rollputSpec = AssembleRolloutSpec(
                    envName,
                    simplifiedRegion,
                    description: $"[{envName}][{region}] {description}",
                    ev2Options.NotificationEmail);
                File.WriteAllText(Path.Combine(outputDirectory, ArtifactConstants.RolloutSpecFileName(targetEnvironment.EnvironmentName, simplifiedRegion)), rollputSpec.ToJsonString(indented: true));

                var parameterFileName = ArtifactConstants.RolloutParametersFileName(targetEnvironment.EnvironmentName.ToString(), simplifiedRegion);
                var parameterFileDir = Path.Combine(outputDirectory, "parameters", targetEnvironment.EnvironmentName.ToString());
                Directory.CreateDirectory(parameterFileDir);
                var parameterFilePath = Path.Combine(parameterFileDir, parameterFileName);
                var parameters = AssemblyRolloutParameters(
                    envName,
                    region,
                    entryScript,
                    targetEnvironment.RunnerInformation,
                    uploadImageToACR ? ev2Options.OneBranchContainerImages : null);
                File.WriteAllText(parameterFilePath, parameters.ToJsonString(indented: true));
            }
        }

        /// <summary>
        /// Generates scope binding file template. Navigate to below links for details on working of the code.
        /// https://msazure.visualstudio.com/Azure-Express/_git/Samples?path=/ServiceGroupRoot/ScopeBindings.json
        /// https://ev2docs.azure.net/features/parameterization/dynamicbindings.html
        /// </summary>
        private static ScopeBindingsModel AssembleScopeBindings()
        {
            var bindings = new List<Bindings>()
            {
                new Bindings()
                {
                    Find = "__MDM_IMAGE_VERSION__",
                    ReplaceWith = "latest",
                },
                new Bindings()
                {
                    Find = "__MDSD_IMAGE_VERSION__",
                    ReplaceWith = "latest",
                },
                new Bindings()
                {
                    Find = "__FLUENTD_IMAGE_VERSION__",
                    ReplaceWith = "latest",
                },
                new Bindings()
                {
                    Find = "__AZSECPACK_IMAGE_VERSION__",
                    ReplaceWith = "latest",
                },
                new Bindings()
                {
                    Find = "__PROMMDMCONVERTER_IMAGE_VERSION__",
                    ReplaceWith = "latest",
                },
            };

            var scopeBindings = new ScopeBindingsModel()
            {
                ScopeBindings = new List<ScopeBindings>()
                {
                    new ScopeBindings()
                    {
                        ScopeTagName = ArtifactConstants.c_LiftrCommonImageVersions,
                        Bindings = bindings,
                    },
                },
            };

            return scopeBindings;
        }

        private static ServiceModel AssembleServiceModel(
            string envName,
            IEnumerable<string> regions,
            string serviceTreeName,
            Guid serviceTreeId,
            string ev2ShellLocation,
            Guid ev2ShellSubscriptionId,
            Func<string, string, string> rolloutParameterPathGenerator)
        {
            var ev2RGName = $"liftr-ev2-shell-{ev2ShellLocation.ToLowerInvariant().RemoveWhitespace()}-rg";
            regions = regions.Select(r => ToSimpleName(r));

            var serviceModel = new ServiceModel()
            {
                ServiceMetadata = new ServiceMetadata()
                {
                    ServiceGroup = serviceTreeName,
                    ServiceIdentifier = serviceTreeId.ToString(),
                    Environment = envName,
                },
                ServiceResourceGroupDefinitions = new List<ServiceResourceGroupDefinition>()
                    {
                        new ServiceResourceGroupDefinition()
                        {
                            Name = ArtifactConstants.c_EV2ServiceResourceGroupDefinitionName,
                            ServiceResourceDefinitions = new List<ServiceResourceDefinition>()
                            {
                                new ServiceResourceDefinition()
                                {
                                    Name = "ShellExtension",
                                    ComposedOf = new CompositionParts()
                                    {
                                        Extension = new ExtensionCompositionPart()
                                        {
                                            Shell = new List<ExtensionItem>()
                                            {
                                                new ExtensionItem()
                                                {
                                                    Type = ArtifactConstants.c_EV2ShellExtensionType,
                                                    Properties = new Dictionary<string, string>()
                                                    {
                                                        ["ImageName"] = "adm-ubuntu-1804-l",
                                                        ["ImageVersion"] = "v14", // https://ev2docs.azure.net/features/extensibility/shell/intro.html#supported-images-and-availability
                                                    },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    }, // ServiceResourceGroupDefinitions
                ServiceResourceGroups = new List<ServiceResourceGroup>()
                    {
                        new ServiceResourceGroup()
                        {
                            AzureResourceGroupName = ev2RGName,
                            Location = ev2ShellLocation,
                            InstanceOf = ArtifactConstants.c_EV2ServiceResourceGroupDefinitionName,
                            AzureSubscriptionId = ev2ShellSubscriptionId,
                            ScopeTags = new List<ScopeTag>()
                            {
                                new ScopeTag()
                                {
                                    Name = ArtifactConstants.c_LiftrCommonImageVersions,
                                },
                            },
                            ServiceResources = regions.Select(r => new ServiceResource()
                            {
                                Name = "ShellExt" + r,
                                InstanceOf = "ShellExtension",
                                RolloutParametersPath = rolloutParameterPathGenerator(envName, r),
                            }),
                        },
                    },
            };

            return serviceModel;
        }

        private static RolloutSpecification AssembleRolloutSpec(
            string envName,
            string region,
            string description,
            string email)
        {
            var spec = new RolloutSpecification()
            {
                RolloutMetadata = new RolloutMetadata()
                {
                    ServiceModelPath = $"ServiceModel.{envName}.json",
                    ScopeBindingsPath = ArtifactConstants.c_ScopeBindingsFileName,
                    Name = description,
                    RolloutType = RolloutType.Major,
                    BuildSource = new BuildSource()
                    {
                        Parameters = new Dictionary<string, string>()
                        {
                            ["VersionFile"] = "version.txt",
                        },
                    },
                    Notification = new RolloutNotification()
                    {
                        Email = new EmailNotification()
                        {
                            To = email,
                        },
                    },
                }, // RolloutMetadata
                OrchestratedSteps = new List<RolloutStep>()
                {
                    new RolloutStep()
                    {
                        Name = "Run EV2 shell extension",
                        TargetType = RolloutTarget.ServiceResource,
                        TargetName = "ShellExt" + region,
                        Actions = new List<string>()
                        {
                            "Shell/liftr-shell-action",
                        },
                    },
                },
            };

            return spec;
        }

        private static RolloutParameters AssemblyRolloutParameters(
            string envName,
            string region,
            string entryScript,
            EV2RunnerInfomation runnerInfo,
            string[] oneBranchContainerImages)
        {
            if (string.IsNullOrEmpty(envName))
            {
                throw new ArgumentNullException(nameof(envName));
            }

            if (string.IsNullOrEmpty(region))
            {
                throw new ArgumentNullException(nameof(region));
            }

            var envVariables = new List<ShellEnvironmentVariable>()
            {
                new ShellEnvironmentVariable()
                {
                    Name = "ASPNETCORE_ENVIRONMENT",
                    Value = envName,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "APP_ASPNETCORE_ENVIRONMENT",
                    Value = envName,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "RunnerSPNObjectId",
                    Value = runnerInfo.UserAssignedManagedIdentityObjectId.ToString(),
                },
                new ShellEnvironmentVariable()
                {
                    Name = "MDM_IMAGE_VERSION",
                    Value = "__MDM_IMAGE_VERSION__",
                    EnableScopeTagBindings = true,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "MDSD_IMAGE_VERSION",
                    Value = "__MDSD_IMAGE_VERSION__",
                    EnableScopeTagBindings = true,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "FLUENTD_IMAGE_VERSION",
                    Value = "__FLUENTD_IMAGE_VERSION__",
                    EnableScopeTagBindings = true,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "AZSECPACK_IMAGE_VERSION",
                    Value = "__AZSECPACK_IMAGE_VERSION__",
                    EnableScopeTagBindings = true,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "PROMMDMCONVERTER_IMAGE_VERSION",
                    Value = "__PROMMDMCONVERTER_IMAGE_VERSION__",
                    EnableScopeTagBindings = true,
                },
            };

            if (!region.OrdinalEquals("Global"))
            {
                envVariables.Add(new ShellEnvironmentVariable()
                {
                    Name = "REGION",
                    Value = ToSimpleName(region),
                });
                envVariables.Add(new ShellEnvironmentVariable()
                {
                    Name = "GenevaParametersFile",
                    Value = $"geneva.{ToSimpleName(envName)}.values.yaml",
                });
            }

            if (oneBranchContainerImages?.Length > 0)
            {
                foreach (var image in oneBranchContainerImages)
                {
                    var envVariable = new ShellEnvironmentVariable()
                    {
                        Name = image.Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase), // we cannot have '-' in environment variable.
                        AsSecureValue = true,
                        Reference = new ParameterReference()
                        {
                            Path = $"docker-images/{image}.tar.gz",
                        },
                    };

                    envVariables.Add(envVariable);
                }
            }

            var shellLaunch = new ShellLaunch()
            {
                Command = new List<string>()
                {
                    entryScript,
                },
                Identity = new ShellIdentity()
                {
                    Type = "UserAssigned",
                    UserAssignedIdentities = new List<string>()
                    {
                        runnerInfo.UserAssignedManagedIdentityResourceId,
                    },
                },
            };

            shellLaunch.EnvironmentVariables = envVariables;
            var parameters = new RolloutParameters()
            {
                ShellExtensions = new List<Shell>()
                {
                    new Shell()
                    {
                        Name = "liftr-shell-action",
                        Type = ArtifactConstants.c_EV2ShellExtensionType,
                        Properties = new ShellProperties()
                        {
                            MaxExecutionTime = "PT12H",
                        },
                        Package = new ShellPackage()
                        {
                            Reference = new ParameterReference()
                            {
                                Path = "liftr-deployment.tar",
                            },
                        },
                        Launch = shellLaunch,
                    },
                },
            };

            return parameters;
        }

        private static RolloutParameters AssembleImageBuilderRolloutParameters(
            ImageOptions options,
            string configPath,
            string managedIdentityResourceId,
            Guid managedIdentityObjectId,
            CloudType cloud,
            string entryScript,
            string[] oneBranchContainerImages)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var envVariables = new List<ShellEnvironmentVariable>()
            {
                new ShellEnvironmentVariable()
                {
                    Name = "ImageName",
                    Value = options.ImageName,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "SourceImage",
                    Value = options.SourceImage.ToString(),
                },
                new ShellEnvironmentVariable()
                {
                    Name = "ConfigurationPath",
                    Value = configPath,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "RunnerSPNObjectId",
                    Value = managedIdentityObjectId.ToString(),
                },
                new ShellEnvironmentVariable()
                {
                    Name = "Cloud",
                    Value = cloud.ToString(),
                },
                new ShellEnvironmentVariable()
                {
                    Name = "MDM_IMAGE_VERSION",
                    Value = "__MDM_IMAGE_VERSION__",
                    EnableScopeTagBindings = true,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "MDSD_IMAGE_VERSION",
                    Value = "__MDSD_IMAGE_VERSION__",
                    EnableScopeTagBindings = true,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "FLUENTD_IMAGE_VERSION",
                    Value = "__FLUENTD_IMAGE_VERSION__",
                    EnableScopeTagBindings = true,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "AZSECPACK_IMAGE_VERSION",
                    Value = "__AZSECPACK_IMAGE_VERSION__",
                    EnableScopeTagBindings = true,
                },
                new ShellEnvironmentVariable()
                {
                    Name = "PROMMDMCONVERTER_IMAGE_VERSION",
                    Value = "__PROMMDMCONVERTER_IMAGE_VERSION__",
                    EnableScopeTagBindings = true,
                },
            };

            if (oneBranchContainerImages?.Length > 0)
            {
                foreach (var image in oneBranchContainerImages)
                {
                    var envVariable = new ShellEnvironmentVariable()
                    {
                        Name = image.Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase), // we cannot have '-' in environment variable.
                        AsSecureValue = true,
                        Reference = new ParameterReference()
                        {
                            Path = $"docker-images/{image}.tar.gz",
                        },
                    };

                    envVariables.Add(envVariable);
                }
            }

            var shellLaunch = new ShellLaunch()
            {
                Command = new List<string>()
                {
                    entryScript,
                },
                Identity = new ShellIdentity()
                {
                    Type = "UserAssigned",
                    UserAssignedIdentities = new List<string>()
                    {
                        managedIdentityResourceId,
                    },
                },
            };

            shellLaunch.EnvironmentVariables = envVariables;

            var parameters = new RolloutParameters()
            {
                ShellExtensions = new List<Shell>()
                {
                    new Shell()
                    {
                        Name = "liftr-shell-action",
                        Type = ArtifactConstants.c_EV2ShellExtensionType,
                        Properties = new ShellProperties()
                        {
                            MaxExecutionTime = "PT120M",
                        },
                        Package = new ShellPackage()
                        {
                            Reference = new ParameterReference()
                            {
                                Path = "ev2-extension.tar",
                            },
                        },
                        Launch = shellLaunch,
                    },
                },
            };

            return parameters;
        }

        private static string ToSimpleName(string region)
        {
            return region.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
        }

        private static string GetCDPxVersionText()
        {
            // https://onebranch.visualstudio.com/Pipeline/_wiki/wikis/Pipeline.wiki/325/Versioning
            var numericVersion = Environment.GetEnvironmentVariable("CDP_PACKAGE_VERSION_NUMERIC");
            if (string.IsNullOrEmpty(numericVersion))
            {
                return string.Empty;
            }
            else
            {
                return $" [Version: {numericVersion}]";
            }
        }
    }
}
