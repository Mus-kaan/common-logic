//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Blob
{
    public static class BlobContainerClientExtensions
    {
        /// <summary>
        /// The List Blobs operation returns a list of the blobs under the specified container.
        /// For more information, see https://docs.microsoft.com/rest/api/storageservices/list-blobs.
        /// </summary>
        /// <param name="blobContainer"></param>
        /// <param name="traits">Specifies trait options for shaping the blobs.</param>
        /// <param name="states">Specifies state options for filtering the blobs.</param>
        /// <param name="delimiter">A delimiter that can be used to traverse a virtual hierarchy of blobs as though
        ///     it were a file system. The delimiter may be a single character or a string. Azure.Storage.Blobs.Models.BlobHierarchyItem.Prefix
        ///     will be returned in place of all blobs whose names begin with the same substring
        ///     up to the appearance of the delimiter character. The value of a prefix is substring+delimiter,
        ///     where substring is the common substring that begins one or more blob names, and
        ///     delimiter is the value of delimiter. You can use the value of prefix to make
        ///     a subsequent call to list the blobs that begin with this prefix, by specifying
        ///     the value of the prefix for the prefix. Note that each BlobPrefix element returned
        ///     counts toward the maximum result, just as each Blob element does.</param>
        /// <param name="prefix">Specifies a string that filters the results to return only blobs whose name begins with the specified prefix.</param>
        /// <param name="cancellationToken">Optional System.Threading.CancellationToken to propagate notifications that the operation should be cancelled.</param>
        /// <returns>A list of blobs in the container.</returns>
        public static async Task<List<BlobHierarchyItem>> ListBlobsByHierarchyAsync(
            this BlobContainerClient blobContainer,
            BlobTraits traits = BlobTraits.None,
            BlobStates states = BlobStates.None,
            string delimiter = null,
            string prefix = null,
            CancellationToken cancellationToken = default)
        {
            if (blobContainer == null)
            {
                throw new ArgumentNullException(nameof(blobContainer));
            }

            return await ToListAsync(blobContainer.GetBlobsByHierarchyAsync(traits, states, delimiter, prefix, cancellationToken));
        }

        private static Task<List<BlobHierarchyItem>> ToListAsync(AsyncPageable<BlobHierarchyItem> source, CancellationToken cancellationToken = default)
        {
            return ToListAsync<BlobHierarchyItem>(source, cancellationToken);
        }

        private static async Task<List<T>> ToListAsync<T>(AsyncPageable<T> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            List<T> results = new List<T>();

            await foreach (var page in source)
            {
                results.Add(page);
            }

            return results;
        }
    }
}
