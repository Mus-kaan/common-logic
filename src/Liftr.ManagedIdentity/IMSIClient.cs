//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.ManagedIdentity.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ManagedIdentity
{
    public interface IMSIClient : IDisposable
    {
        /// <summary>
        /// Request to retrieve system assigned and user assigned identity credentials/metadata for a given resource.
        /// </summary>
        /// <param name="identityUrl">The x-ms-identity-url header value returned from ARM that contains the resource path. This is the MSI RP location to
        /// which we can POST the metadata requests.</param>
        /// <param name="userAssignedIdentityResourceIds">The list of user assigned identity resource Ids for which metadata has to be fetched.
        /// If this list is empty, metadata for only system assigned identity will be fetched.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The metadata that contains credentials and other information of identity for both system and user assigned identities.</returns>
        Task<IdentityMetadata> GetIdentityMetadataAsync(
            string identityUrl,
            IEnumerable<string> userAssignedIdentityResourceIds,
            CancellationToken cancellationToken = default);
    }
}
