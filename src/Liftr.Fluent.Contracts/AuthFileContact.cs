//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;

namespace Microsoft.Liftr.Fluent.Contracts
{
    public class AuthFileContact
    {
        public AuthFileContact()
        {
        }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string ServicePrincipalObjectId { get; set; }

        public static AuthFileContact FromFile(string path)
        {
            var content = File.ReadAllText(path);
            var result = content.FromJson<AuthFileContact>();
            if (string.IsNullOrEmpty(result.ServicePrincipalObjectId))
            {
                throw new InvalidOperationException("Please manually fill in the value of 'ServicePrincipalObjectId'");
            }

            return result;
        }
    }
}
