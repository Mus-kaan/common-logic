//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal partial class LiftrAzure
    {
        #region Resource Group
        public async Task<IResourceGroup> GetOrCreateResourceGroupAsync(
            Region location,
            string rgName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            var rg = await GetResourceGroupAsync(rgName, cancellationToken);

            if (rg == null)
            {
                rg = await CreateResourceGroupAsync(location, rgName, tags, cancellationToken);
            }

            return rg;
        }

        public async Task<IResourceGroup> CreateResourceGroupAsync(
            Region location,
            string rgName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("Creating a resource group with name: {rgName}", rgName);
            var rg = await FluentClient
                .ResourceGroups
                .Define(rgName)
                .WithRegion(location)
                .WithTags(tags)
                .CreateAsync(cancellationToken);
            _logger.Information("Created a resource group with Id:{resourceId}", rg.Id);
            return rg;
        }

        public IResourceGroup CreateResourceGroup(
            Region location,
            string rgName,
            IDictionary<string, string> tags)
        {
            _logger.Information("Creating a resource group with name: {rgName}", rgName);
            var rg = FluentClient
                .ResourceGroups
                .Define(rgName)
                .WithRegion(location)
                .WithTags(tags)
                .Create();
            _logger.Information("Created a resource group with Id:{resourceId}", rg.Id);
            return rg;
        }

        public async Task<IResourceGroup> GetResourceGroupAsync(string rgName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Information("Getting resource group with name: {rgName}", rgName);
                var rg = await FluentClient
                .ResourceGroups
                .GetByNameAsync(rgName, cancellationToken);

                if (rg != null)
                {
                    _logger.Information("Retrieved the Resource Group with Id: {resourceId}", rg.Id);
                    return rg;
                }
            }
            catch (CloudException ex) when (ex.IsNotFound())
            {
            }

            return null;
        }

        public async Task DeleteResourceGroupAsync(string rgName, bool noThrow = false, CancellationToken cancellationToken = default)
        {
            _logger.Information("Deleting resource group with name: " + rgName);
            try
            {
                await FluentClient
                .ResourceGroups
                .DeleteByNameAsync(rgName, cancellationToken);

                _logger.Information("Finished deleting resource group with name: " + rgName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Cannot delete resource group with name {rgName}", rgName);
                if (!noThrow)
                {
                    throw;
                }
            }
        }

        public void DeleteResourceGroup(string rgName, bool noThrow = false)
        {
            _logger.Information("Deleting resource group with name: " + rgName);
            try
            {
                FluentClient
                .ResourceGroups
                .DeleteByName(rgName);
                _logger.Information("Finished deleting resource group with name: " + rgName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Cannot delete resource group with name {rgName}", rgName);
                if (!noThrow)
                {
                    throw;
                }
            }
        }

        public async Task DeleteResourceGroupWithTagAsync(string tagName, string tagValue, Func<IReadOnlyDictionary<string, string>, bool> tagsFilter = null, CancellationToken cancellationToken = default)
        {
            _logger.Information($"Listing resource groups in subscription: {FluentClient.SubscriptionId}");
            var rgs = await FluentClient
                .ResourceGroups
                .ListByTagAsync(tagName, tagValue, loadAllPages: true, cancellationToken: cancellationToken);
            _logger.Information("There are {@rgCount} with tagName {@tagName} and {@tagValue}.", rgs.Count(), tagName, tagValue);

            List<Task> tasks = new List<Task>();
            foreach (var rg in rgs)
            {
                if (tagsFilter == null || tagsFilter.Invoke(rg.Tags) == true)
                {
                    tasks.Add(DeleteResourceGroupAsync(rg.Name, noThrow: true, cancellationToken));
                }
            }

            await Task.WhenAll(tasks);
        }

        public async Task DeleteResourceGroupWithPrefixAsync(string rgNamePrefix, CancellationToken cancellationToken = default)
        {
            _logger.Information($"Listing resource groups in subscription: {FluentClient.SubscriptionId}");
            var rgs = await FluentClient
                .ResourceGroups
                .ListAsync(loadAllPages: true, cancellationToken: cancellationToken);

            var toDelete = rgs.Where(rg => rg.Name.OrdinalStartsWith(rgNamePrefix));

            _logger.Information("There are {toDeletCount} resource groups with prefix {rgPrefix} in total {rgCount}.", toDelete.Count(), rgNamePrefix, rgs.Count());

            List<Task> tasks = new List<Task>();
            foreach (var rg in toDelete)
            {
                tasks.Add(DeleteResourceGroupAsync(rg.Name, noThrow: true, cancellationToken: cancellationToken));
            }

            await Task.WhenAll(tasks);
        }

        public async Task DeleteResourceGroupWithNamePartAsync(string rgNamePart, CancellationToken cancellationToken = default)
        {
            _logger.Information($"Listing resource groups in subscription: {FluentClient.SubscriptionId}");
            var rgs = await FluentClient
                .ResourceGroups
                .ListAsync(loadAllPages: true, cancellationToken: cancellationToken);

            var toDelete = rgs.Where(rg => rg.Name.OrdinalContains(rgNamePart));

            _logger.Information("There are {toDeletCount} resource groups with name part {rgPrefix} in total {rgCount}.", toDelete.Count(), rgNamePart, rgs.Count());

            List<Task> tasks = new List<Task>();
            foreach (var rg in toDelete)
            {
                tasks.Add(DeleteResourceGroupAsync(rg.Name, noThrow: true, cancellationToken: cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
        #endregion Resource Group
    }
}
