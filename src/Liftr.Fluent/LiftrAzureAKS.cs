//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal partial class LiftrAzure
    {
        #region AKS
        public async Task<IKubernetesCluster> CreateAksClusterAsync(
            Region region,
            string rgName,
            string aksName,
            string rootUserName,
            string sshPublicKey,
            ContainerServiceVMSizeTypes vmSizeType,
            string k8sVersion,
            int vmCount,
            string outboundIPId,
            IDictionary<string, string> tags,
            ISubnet subnet = null,
            string agentPoolProfileName = "ap",
            bool supportAvailabilityZone = false,
            CancellationToken cancellationToken = default)
        {
            Regex rx = new Regex(@"^[a-z][a-z0-9]{0,11}$");
            if (!rx.IsMatch(agentPoolProfileName))
            {
                throw new ArgumentException("Agent pool profile name does not match pattern '^[a-z][a-z0-9]{0,11}$'");
            }

            _logger.Information($"Availability Zone Support is set {supportAvailabilityZone} for the Kuberenetes Cluster");
            _logger.Information("Creating a Kubernetes cluster of version {kubernetesVersion} with name {aksName} ...", k8sVersion, aksName);
            _logger.Information($"Outbound IP {outboundIPId} is added to AKS cluster ARM Template...");

            using var ops = _logger.StartTimedOperation(nameof(CreateAksClusterAsync));
            try
            {
                var templateContent = AKSHelper.GenerateAKSTemplate(
                    region,
                    aksName,
                    k8sVersion,
                    rootUserName,
                    sshPublicKey,
                    vmSizeType.Value,
                    vmCount,
                    agentPoolProfileName,
                    tags,
                    supportAvailabilityZone,
                    outboundIPId,
                    subnet);

                await CreateDeploymentAsync(region, rgName, templateContent, cancellationToken: cancellationToken);

                var k8s = await GetAksClusterAsync(rgName, aksName, cancellationToken);

                _logger.Information("Created Kubernetes cluster with resource Id {resourceId}", k8s.Id);
                return k8s;
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                _logger.Error(ex, "AKS creation failed.");
                throw;
            }
        }

        public async Task<IKubernetesCluster> GetAksClusterAsync(string aksResourceId, CancellationToken cancellationToken = default)
        {
            return await FluentClient
                .KubernetesClusters
                .GetByIdAsync(aksResourceId, cancellationToken);
        }

        public Task<IKubernetesCluster> GetAksClusterAsync(string rgName, string aksName, CancellationToken cancellationToken = default)
        {
            var aksId = $"subscriptions/{FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.ContainerService/managedClusters/{aksName}";
            return GetAksClusterAsync(aksId, cancellationToken);
        }

        public async Task<IEnumerable<IKubernetesCluster>> ListAksClusterAsync(string rgName, CancellationToken cancellationToken = default)
        {
            _logger.Information($"Listing Aks cluster in resource group {rgName} ...");
            return await FluentClient
                .KubernetesClusters
                .ListByResourceGroupAsync(rgName, loadAllPages: true, cancellationToken: cancellationToken);
        }

        public async Task<string> GetAKSMIAsync(string rgName, string aksName, CancellationToken cancellationToken = default)
        {
            // https://docs.microsoft.com/en-us/azure/aks/use-managed-identity
            _logger.Information($"Getting the AKS control plane managed idenity of cluster '{aksName}'");
            var aksId = $"subscriptions/{FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.ContainerService/managedClusters/{aksName}";
            var aksContent = await GetResourceAsync(aksId, "2020-04-01", cancellationToken);
            if (string.IsNullOrEmpty(aksContent))
            {
                return null;
            }

            try
            {
                dynamic aksObject = JObject.Parse(aksContent);
                return aksObject.identity.principalId;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return null;
            }
        }

        public async Task<IEnumerable<IIdentity>> ListAKSMCMIAsync(
            string AKSRGName,
            string AKSName,
            Region location,
            CancellationToken cancellationToken = default)
        {
            // https://docs.microsoft.com/en-us/azure/aks/use-managed-identity
            _logger.Information($"Listing the AKS managed identities in 'MC_' resource group '{AKSRGName}'");
            var mcRG = NamingContext.AKSMCResourceGroupName(AKSRGName, AKSName, location);
            return await FluentClient.Identities.ListByResourceGroupAsync(mcRG, loadAllPages: true, cancellationToken: cancellationToken);
        }
        #endregion AKS
    }
}
