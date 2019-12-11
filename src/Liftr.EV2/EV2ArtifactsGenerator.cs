//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.EV2.Contracts;
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

        public void GenerateArtifacts(EV2Options ev2Options, string outputDirectory)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            try
            {
                ev2Options.CheckValid();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw;
            }

            Directory.CreateDirectory(outputDirectory);

            GenerateGlobalArtifacts(ev2Options, Path.Combine(outputDirectory, ArtifactConstants.c_GlobalFolderName));
            GenerateRegionDataArtifacts(ev2Options, Path.Combine(outputDirectory, ArtifactConstants.c_RegionalDataFolderName));
            GenerateRegionComputeArtifacts(ev2Options, Path.Combine(outputDirectory, ArtifactConstants.c_RegionalComputeFolderName));
            GenerateAKSAppArtifacts(ev2Options, Path.Combine(outputDirectory, ArtifactConstants.c_ApplicationFolderName));
        }

        public void GenerateGlobalArtifacts(EV2Options ev2Options, string outputDirectory)
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
                targetEnvironment.Regions = regions;

                GenerateEnvironmentArtifacts(
                    ev2Options,
                    targetEnvironment,
                    outputDirectory,
                    description: "Create or update global resources",
                    entryScript: "1_ManageGlobalResources.sh");
            }

            _logger.Information("Generated EV2 global deployment artifacts and stored in directory: {outputDirectory}", outputDirectory);
        }

        public void GenerateRegionDataArtifacts(EV2Options ev2Options, string outputDirectory)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            foreach (var targetEnvironment in ev2Options.TargetEnvironments)
            {
                GenerateEnvironmentArtifacts(
                    ev2Options,
                    targetEnvironment,
                    outputDirectory,
                    description: "Create or update regional data resources",
                    entryScript: "2_ManageRegionalData.sh");
            }

            _logger.Information("Generated EV2 regional data deployment artifacts and stored in directory: {outputDirectory}", outputDirectory);
        }

        public void GenerateRegionComputeArtifacts(EV2Options ev2Options, string outputDirectory)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            foreach (var targetEnvironment in ev2Options.TargetEnvironments)
            {
                GenerateEnvironmentArtifacts(
                    ev2Options,
                    targetEnvironment,
                    outputDirectory,
                    description: "Create or update regional AKS and deploy infrastructure AKS applications (Geneva, Pod identity, Nginx Ingress)",
                    entryScript: "3_ManageRegionalCompute.sh");
            }

            _logger.Information("Generated EV2 regional compute deployment artifacts and stored in directory: {outputDirectory}", outputDirectory);
        }

        public void GenerateAKSAppArtifacts(EV2Options ev2Options, string outputDirectory)
        {
            if (ev2Options == null)
            {
                throw new ArgumentNullException(nameof(ev2Options));
            }

            foreach (var targetEnvironment in ev2Options.TargetEnvironments)
            {
                GenerateEnvironmentArtifacts(
                    ev2Options,
                    targetEnvironment,
                    outputDirectory,
                    description: "Deploy AKS applications using helm charts",
                    entryScript: "4_DeployAKSApp.sh");
            }

            _logger.Information("Generated EV2 AKS application deployment artifacts and stored in directory: {outputDirectory}", outputDirectory);
        }

        public void GenerateEnvironmentArtifacts(
            EV2Options ev2Options,
            TargetEnvironment targetEnvironment,
            string outputDirectory,
            string description,
            string entryScript)
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

            var envName = targetEnvironment.EnvironmentName.ToString();

            _logger.Information("Generating EV2 artifacts for environment {envName} and regions: {@regions}", envName, targetEnvironment.Regions);

            var serviceModel = AssembleServiceModel(
                envName,
                targetEnvironment.Regions,
                ev2Options.ServiceTreeName,
                ev2Options.ServiceTreeId,
                targetEnvironment.RunnerInformation.Location,
                targetEnvironment.RunnerInformation.SubscriptionId);
            File.WriteAllText(Path.Combine(outputDirectory, ArtifactConstants.ServiceModelFileName(targetEnvironment.EnvironmentName)), serviceModel.ToJsonString(indented: true));

            foreach (var region in targetEnvironment.Regions)
            {
                var simplifiedRegion = ToSimpleName(region);
                var rollputSpec = AssembleRolloutSpec(
                    envName,
                    simplifiedRegion,
                    description: $"[{region}] {description}",
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
                    targetEnvironment.RunnerInformation.UserAssignedManagedIdentityResourceId);
                File.WriteAllText(parameterFilePath, parameters.ToJsonString(indented: true));
            }
        }

        public static ServiceModel AssembleServiceModel(
            string envName,
            IEnumerable<string> regions,
            string serviceTreeName,
            Guid serviceTreeId,
            string ev2ShellLocation,
            Guid ev2ShellSubscriptionId)
        {
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
                                                        ["ImageVersion"] = "v2",
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
                            AzureResourceGroupName = "liftr-ev2-shell-ext-rg",
                            Location = ev2ShellLocation,
                            InstanceOf = ArtifactConstants.c_EV2ServiceResourceGroupDefinitionName,
                            AzureSubscriptionId = ev2ShellSubscriptionId,
                            ServiceResources = regions.Select(r => new ServiceResource()
                            {
                                Name = "ShellExt" + r,
                                InstanceOf = "ShellExtension",
                                RolloutParametersPath = ArtifactConstants.RolloutParametersPath(envName, r),
                            }),
                        },
                    },
            };

            return serviceModel;
        }

        public static RolloutSpecification AssembleRolloutSpec(
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

        public static RolloutParameters AssemblyRolloutParameters(
            string envName,
            string region,
            string entryScript,
            string runnerManagedIdentityId)
        {
            if (string.IsNullOrEmpty(envName))
            {
                throw new ArgumentNullException(nameof(envName));
            }

            if (string.IsNullOrEmpty(region))
            {
                throw new ArgumentNullException(nameof(region));
            }

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
                            MaxExecutionTime = "PT70M",
                        },
                        Package = new ShellPackage()
                        {
                            Reference = new ParameterReference()
                            {
                                Path = "liftr-deployment.tar",
                            },
                        },
                        Launch = new ShellLaunch()
                        {
                            Command = new List<string>()
                            {
                                entryScript,
                            },
                            EnvironmentVariables = new List<ShellEnvironmentVariable>()
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
                                    Name = "REGION",
                                    Value = region,
                                },
                                new ShellEnvironmentVariable()
                                {
                                    Name = "gcs_region",
                                    Value = ToSimpleName(region),
                                },
                                new ShellEnvironmentVariable()
                                {
                                    Name = "GenevaParametersFile",
                                    Value = $"geneva.{ToSimpleName(envName)}.values.yaml",
                                },
                            },
                            Identity = new ShellIdentity()
                            {
                                Type = "UserAssigned",
                                UserAssignedIdentities = new List<string>()
                                {
                                    runnerManagedIdentityId,
                                },
                            },
                        },
                    },
                },
            };

            return parameters;
        }

        private static string ToSimpleName(string region)
        {
            return region.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
        }
    }
}
