//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;

namespace Microsoft.Liftr.Fluent.Contracts
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "<Pending>")]
    public class AuthFileContract
    {
        public AuthFileContract()
        {
        }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string SubscriptionId { get; set; }

        public string TenantId { get; set; }

        public string ActiveDirectoryEndpointUrl { get; set; }

        public string ResourceManagerEndpointUrl { get; set; }

        public string ActiveDirectoryGraphResourceId { get; set; }

        public string SqlManagementEndpointUrl { get; set; }

        public string GalleryEndpointUrl { get; set; }

        public string ManagementEndpointUrl { get; set; }

        public string ServicePrincipalObjectId { get; set; }

        public static AuthFileContract FromFileContent(string content)
        {
            var result = content.FromJson<AuthFileContract>();
            if (string.IsNullOrEmpty(result.ServicePrincipalObjectId))
            {
                throw new InvalidOperationException("Please manually fill in the value of 'ServicePrincipalObjectId'");
            }

            return result;
        }

        public static AuthFileContract FromFile(string path)
        {
            var content = File.ReadAllText(path);
            return FromFileContent(content);
        }
    }
}
