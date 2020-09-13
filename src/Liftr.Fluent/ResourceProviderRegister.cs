//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public class ResourceProviderRegister
    {
        private const string c_providerRegisteredMarker = "\"registrationState\":\"Registered\"";
        private const string c_featureRegisteredMarker = "\"Registered\"";
        private readonly ILogger _logger;

        public ResourceProviderRegister(Serilog.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RegisterBakeImageProvidersAndFeaturesAsync(ILiftrAzure liftrAzure)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            var providers = new List<string>(s_commonProviderList);
            providers.Add("Microsoft.ManagedIdentity");
            providers.Add("Microsoft.VirtualMachineImages");
            await RegisterProvidersAsync(liftrAzure, s_commonProviderList);

            bool registered1 = await RegisterFeatureAsync(liftrAzure, "Microsoft.Compute", "GalleryPreview");
            bool registered2 = await RegisterFeatureAsync(liftrAzure, "Microsoft.VirtualMachineImages", "VirtualMachineTemplatePreview");

            if (!registered1 || !registered2)
            {
                _logger.Information("Wait for 5 minutes to make sure the feature is registered.");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }

        public async Task RegisterImportImageProvidersAndFeaturesAsync(ILiftrAzure liftrAzure)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            await RegisterProvidersAsync(liftrAzure, s_commonProviderList);
        }

        public async Task RegisterGenericHostingProvidersAndFeaturesAsync(ILiftrAzure liftrAzure)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            await RegisterProvidersAsync(liftrAzure, s_commonProviderList);
            await RegisterProvidersAsync(liftrAzure, s_genericHostingProviderList);

            bool registered1 = await RegisterFeatureAsync(liftrAzure, "Microsoft.Compute", "GalleryPreview");

            if (!registered1)
            {
                _logger.Information("Wait for 5 minutes to make sure the feature is registered.");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }

        private async Task RegisterProvidersAsync(ILiftrAzure liftrAzure, List<string> providerList)
        {
            bool registering = false;
            try
            {
                foreach (var provider in providerList)
                {
                    var registration = await liftrAzure.RegisterResourceProviderAsync(provider);
                    if (registration.Contains(c_providerRegisteredMarker))
                    {
                        _logger.Debug($"'{provider}' is already registered.");
                    }
                    else
                    {
                        registering = true;
                        _logger.Information($"'{provider}' has not been registered. Wait for the registration to finish.");
                    }
                }

                if (registering)
                {
                    _logger.Information("Wait for 5 minutes to make sure the resource provider is registered.");
                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed at register resource providers.");
                throw;
            }
        }

        private async Task<bool> RegisterFeatureAsync(ILiftrAzure liftrAzure, string provider, string feature)
        {
            bool registered = true;
            var featureRegistration = await liftrAzure.RegisterFeatureAsync(provider, feature);
            if (featureRegistration.Contains(c_featureRegisteredMarker))
            {
                _logger.Debug($"Feature '{feature}' is already registered.");
            }
            else
            {
                registered = false;
                _logger.Information($"Feature '{feature}' has not been registered. Wait for the registration to finish.");
            }

            return registered;
        }

        private static readonly List<string> s_commonProviderList = new List<string>()
        {
            "Microsoft.Compute",
            "Microsoft.Storage",
            "Microsoft.Network",
            "Microsoft.KeyVault",
        };

        private static readonly List<string> s_genericHostingProviderList = new List<string>()
        {
            "Microsoft.ManagedIdentity",
            "Microsoft.EventHub",
            "Microsoft.DocumentDB",
            "Microsoft.DomainRegistration",
            "Microsoft.ContainerService",
            "Microsoft.ContainerRegistry",
            "Microsoft.Insights",
            "Microsoft.OperationalInsights",
            "Microsoft.OperationsManagement",
        };
    }
}
