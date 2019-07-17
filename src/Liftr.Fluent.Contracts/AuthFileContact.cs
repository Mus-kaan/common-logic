//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

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

        public static AuthFileContact FromFile(string path)
        {
            var content = File.ReadAllText(path);
            var result = content.FromJson<AuthFileContact>();
            return result;
        }
    }
}
