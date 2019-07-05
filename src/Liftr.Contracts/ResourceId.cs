//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    public sealed class ResourceId
    {
        private const string c_subscriptions = "subscriptions";
        private const string c_resourceGroups = "resourceGroups";
        private const string c_resourceGroup = "providers";

        private readonly string _resourceIdStr;

        public ResourceId(string resourceId)
        {
            _resourceIdStr = resourceId;

            bool isValidResourceIdFormat = false;
            var parts = resourceId.Split('/');
            if (parts.Length >= 9 && parts.Length % 2 == 1)
            {
                if (parts[1].OrdinalEquals(c_subscriptions) && parts[3].OrdinalEquals(c_resourceGroups) && parts[5].OrdinalEquals(c_resourceGroup))
                {
                    isValidResourceIdFormat = true;
                }
            }

            if (!isValidResourceIdFormat)
            {
                throw new FormatException($"'{resourceId}' is not valid resourceId format.");
            }

            SubscriptionId = parts[2];
            ResourceGroup = parts[4];
            Provider = parts[6];
            ResourceType = parts[7];
            ResourceName = parts[8];

            List<ResourceTypeNamePair> names = new List<ResourceTypeNamePair>();
            for (int i = 7; i < parts.Length; i += 2)
            {
                names.Add(new ResourceTypeNamePair() { ResourceType = parts[i], ResourceName = parts[i + 1] });
            }

            TypedNames = names.ToArray();
        }

        public string SubscriptionId { get; }

        public string ResourceGroup { get; }

        public string Provider { get; }

        public string ResourceType { get; }

        public string ResourceName { get; }

        public ResourceTypeNamePair[] TypedNames { get; }

        public override string ToString() => _resourceIdStr;

#pragma warning disable CA1054 // Uri parameters should not be strings
        public static ResourceId FromResourceUri(string resourceUri)
#pragma warning restore CA1054 // Uri parameters should not be strings
        {
            try
            {
                var resourceId = resourceUri.Substring(resourceUri.OrdinalIndexOf($"/{c_subscriptions}/"));
                return new ResourceId(resourceId);
            }
            catch (Exception ex)
            {
                throw new FormatException($"'{resourceUri}' does not contain valid resource Id part.", ex);
            }
        }
    }
}
