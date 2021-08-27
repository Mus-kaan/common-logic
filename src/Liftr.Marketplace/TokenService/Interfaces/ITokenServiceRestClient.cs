//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.ARM.Interfaces
{
    public interface ITokenServiceRestClient
    {
        /// <summary>
        /// Gets FPA Based token from the Token Service
        /// </summary>
        Task<string> GetTokenAsync();

        /// <summary>
        /// Gets X509Certificate Object from the Token Service
        /// </summary>
        Task<X509Certificate2> GetCertificateAsync();
    }
}
