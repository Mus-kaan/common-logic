//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.Rest.Azure;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr
{
    public static class BlobContainerExtensions
    {
        public static async Task<IBlobContainer> GetOrCreateBlobContainerAsync(this IStorageAccount azureStorAcc, Serilog.ILogger logger, string blobContainerName)
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
                container = await azureStorAcc.Manager.BlobContainers.GetAsync(azureStorAcc.ResourceGroupName, azureStorAcc.Name, blobContainerName);
            }
            catch (CloudException ex) when (ex.IsNotFound())
            {
                logger.Information("Could not find storage container group {storageContainerName} under {rgName}/{storageAccName} ...", blobContainerName, azureStorAcc.ResourceGroupName, azureStorAcc.Name);
                container = await azureStorAcc.CreateBlobContainerAsync(logger, blobContainerName);
            }

            return container;
        }

        public static async Task<IBlobContainer> CreateBlobContainerAsync(this IStorageAccount azureStorAcc, Serilog.ILogger logger, string blobContainerName)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (azureStorAcc == null)
            {
                throw new ArgumentNullException(nameof(azureStorAcc));
            }

            logger.Information("Creating storage container with name {storageContainerName} in {rgName}/{storageAccName} ...", blobContainerName, azureStorAcc.ResourceGroupName, azureStorAcc.Name);
            return await azureStorAcc.Manager.BlobContainers.DefineContainer(blobContainerName)
                        .WithExistingBlobService(azureStorAcc.ResourceGroupName, azureStorAcc.Name)
                        .WithPublicAccess(PublicAccess.None)
                        .CreateAsync();
        }
    }
}