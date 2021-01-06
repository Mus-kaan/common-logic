//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr
{
    public static class MetadataHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1501:Statement should not be on a single line", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public static async Task<string> GetPublicIPAddressAsync(bool noThrow = false)
        {
            string ip = null;
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://api.ipify.org/");
                    ip = await response.Content.ReadAsStringAsync();
                }
            }
            catch { }

            if (!noThrow && string.IsNullOrEmpty(ip))
            {
                throw new InvalidOperationException("Cannot get public IP address.");
            }

            return ip;
        }
    }
}
