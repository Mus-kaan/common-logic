//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Liftr.TokenManager
{
    /// <summary>
    /// Interface to cache AAD tokens, tokens in this cache will be stored untill they are expired
    /// </summary>
    internal interface ITokenCache
    {
        /// <summary>
        /// Retrieves token from cache for given clientId
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        AuthenticationResult GetTokenItem(string clientId);

        /// <summary>
        /// Sets token to cache for given clientId
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="token"></param>
        void SetTokenItem(string clientId, AuthenticationResult token);
    }
}
