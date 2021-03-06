//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;

namespace Microsoft.Liftr.Hosting.Contracts
{
    public class PartnerCredentialUpdateOptions
    {
        /// <summary>
        /// Multi tenant secure sharing app Id.
        /// </summary>
        public string MultiTenantAppId { get; set; }

        /// <summary>
        /// Partner keyvault Endpoint.
        /// </summary>
        public string PartnerKeyvaultEndpoint { get; set; }

        /// <summary>
        /// Secure Sharing App Certificate Subject Name.
        /// </summary>
        public string CertificateSubjectName { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(MultiTenantAppId))
            {
                throw new InvalidHostingOptionException($"{nameof(MultiTenantAppId)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(PartnerKeyvaultEndpoint))
            {
                throw new InvalidHostingOptionException($"{nameof(PartnerKeyvaultEndpoint)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(CertificateSubjectName))
            {
                throw new InvalidHostingOptionException($"{nameof(CertificateSubjectName)} cannot be null or empty.");
            }
        }
    }
}
