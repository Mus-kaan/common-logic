//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.RPaaS
{
    internal static class Utils
    {
        public static string GetMetaRPResourceUrl(string resourceId, string apiVersion)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            if (string.IsNullOrEmpty(apiVersion))
            {
                return resourceId;
            }

            int index = resourceId.IndexOf("?api-version=", StringComparison.CurrentCultureIgnoreCase);
            if (index >= 0)
            {
                return resourceId.Substring(0, index) + "?api-version=" + apiVersion;
            }

            return resourceId + "?api-version=" + apiVersion;
        }
    }
}
