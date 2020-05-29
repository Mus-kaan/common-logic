//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.Rest.Azure;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr
{
    public static class BlobContainerExtensions
    {
        public static async Task<IBlobContainer> GetOrCreateStorageAccContainerAsync(this IStorageAccount azureStorAcc, Serilog.ILogger logger, Region region, string rgName, string storageAccName, string storageContainerName)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (azureStorAcc == null)
            {
                throw new ArgumentNullException(nameof(azureStorAcc));
            }

            IBlobContainer container;
            try
            {
                container = await azureStorAcc.Manager.BlobContainers.GetAsync(rgName, storageAccName, storageContainerName);
            }
            catch (CloudException ex) when (ex.IsNotFound())
            {
                logger.Information("Could not find storage container group {storageContainerName} under {rgName}/{storageAccName} ...", storageContainerName, rgName, storageAccName);
                container = await azureStorAcc.CreateStorageAccContainerAsync(logger, region, rgName, storageAccName, storageContainerName);
            }

            return container;
        }

        public static async Task<IBlobContainer> CreateStorageAccContainerAsync(this IStorageAccount azureStorAcc, Serilog.ILogger logger, Region region, string rgName, string storageAccName, string storageContainerName)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (azureStorAcc == null)
            {
                throw new ArgumentNullException(nameof(azureStorAcc));
            }

            logger.Information("Creating storage container with name {storageContainerName} in {rgName}/{storageAccName} ...", storageContainerName, rgName, storageAccName);
            return await azureStorAcc.Manager.BlobContainers.DefineContainer(storageContainerName)
                        .WithExistingBlobService(rgName, storageAccName)
                        .WithPublicAccess(PublicAccess.None)
                        .CreateAsync();
        }
    }
}