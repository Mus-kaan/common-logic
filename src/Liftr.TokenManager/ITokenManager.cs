//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    /// <summary>
    /// Manages token for AAD client side
    /// </summary>
    public interface ITokenManager
    {
        /// <summary>
        /// Retrieves token for given clientId and clientSecret
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <returns></returns>
        Task<string> GetTokenAsync(string clientId, string clientSecret);
    }
}
