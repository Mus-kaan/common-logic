//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    /// <summary>
    /// Manages token for AAD client side
    /// </summary>
    public interface ITokenManager
    {
        /// <summary>
        /// Retrieves token for given clientId and clientSecret scoped to the given tenant.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="tenantId">If tenant is null, default tenant will be used.</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<string> GetTokenAsync(string clientId, string clientSecret, string tenantId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves certificate from given vault endpoint and returns the token for given clientId and certificate scoped to the given tenant.
        /// </summary>
        /// <param name="keyVaultEndpoint"></param>
        /// <param name="clientId"></param>
        /// <param name="certificateName"></param>
        /// <param name="tenantId">If tenant is null, default tenant will be used</param>
        /// <param name="sendX5c">This parameter enables application developers to achieve easy certificates roll-over
        ///    in Azure AD: setting this parameter to true will send the public certificate
        ///     to Azure AD along with the token request, so that Azure AD can use it to validate
        ///     the subject name based on a trusted issuer policy. This saves the application
        ///     admin from the need to explicitly manage the certificate rollover (either via
        ///    portal or powershell/CLI operation)
        /// https://aadwiki.windows-int.net/index.php?title=Subject_Name_and_Issuer_Authentication </param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<string> GetTokenAsync(Uri keyVaultEndpoint, string clientId, string certificateName, string tenantId = null, bool sendX5c = false, CancellationToken cancellationToken = default);
    }
}
