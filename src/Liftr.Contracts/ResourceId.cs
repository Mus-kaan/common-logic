//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    /// <summary>
    /// https://armwiki.azurewebsites.net/introduction/concepts/resourceids.html
    /// </summary>
    public class ResourceId
    {
        private const string c_subscriptions = "subscriptions";
        private const string c_resourceGroups = "resourceGroups";
        private const string c_providers = "providers";

        private readonly string _resourceIdStr;

        public ResourceId(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            _resourceIdStr = resourceId;

            bool isValidResourceIdFormat = false;
            var parts = resourceId.Split('/');
            if (parts.Length >= 7 && parts.Length % 2 == 1)
            {
                if (parts[1].OrdinalEquals(c_subscriptions) && parts[3].OrdinalEquals(c_resourceGroups) && parts[5].OrdinalEquals(c_providers))
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

            if (parts.Length >= 9)
            {
                ResourceType = parts[7];
                ResourceName = parts[8];
            }

            List<ResourceTypeNamePair> names = new List<ResourceTypeNamePair>();
            for (int i = 7; i < parts.Length; i += 2)
            {
                names.Add(new ResourceTypeNamePair() { ResourceType = parts[i], ResourceName = parts[i + 1] });
            }

            TypedNames = names.ToArray();
        }

        public ResourceId(
            string subscriptionId,
            string resourceGroup,
            string provider,
            string resourceType,
            string resourceName = null,
            string childResourceType = null,
            string childResourceName = null)
        {
            SubscriptionId = subscriptionId;
            ResourceGroup = resourceGroup;
            Provider = provider;
            ResourceType = resourceType;
            ResourceName = resourceName;
            _resourceIdStr = $"/{c_subscriptions}/{SubscriptionId}/{c_resourceGroups}/{ResourceGroup}/{c_providers}/{Provider}";

            List<ResourceTypeNamePair> names = new List<ResourceTypeNamePair>();

            if (!string.IsNullOrEmpty(resourceName))
            {
                names.Add(new ResourceTypeNamePair() { ResourceType = resourceType, ResourceName = resourceName });
                _resourceIdStr = _resourceIdStr + $"/{ResourceType}/{ResourceName}";
            }

            if (!string.IsNullOrEmpty(childResourceType) && !string.IsNullOrEmpty(childResourceName))
            {
                ChildResourceType = childResourceType;
                ChildResourceName = childResourceName;

                names.Add(new ResourceTypeNamePair() { ResourceType = childResourceType, ResourceName = childResourceName });
                _resourceIdStr = _resourceIdStr + $"/{childResourceType}/{childResourceName}";
            }

            TypedNames = names.ToArray();
        }

        public string SubscriptionId { get; }

        public string ResourceGroup { get; }

        public string Provider { get; }

        public string ResourceType { get; }

        public string ResourceName { get; }

        public string ChildResourceType
        {
            get
            {
                if (TypedNames.Length < 2)
                {
                    return null;
                }

                return TypedNames[1].ResourceType;
            }

            private set
            {
            }
        }

        public string ChildResourceName
        {
            get
            {
                if (TypedNames.Length < 2)
                {
                    return null;
                }

                return TypedNames[1].ResourceName;
            }

            private set
            {
            }
        }

        public ResourceTypeNamePair[] TypedNames { get; }

        public override string ToString() => _resourceIdStr;

#pragma warning disable CA1054 // Uri parameters should not be strings
        public static ResourceId FromResourceUri(string resourceUri)
#pragma warning restore CA1054 // Uri parameters should not be strings
        {
            try
            {
                var resourceId = resourceUri?.Substring(resourceUri.OrdinalIndexOf($"/{c_subscriptions}/")) ?? throw new ArgumentNullException(nameof(resourceUri));
                return new ResourceId(resourceId);
            }
            catch (Exception ex)
            {
                throw new FormatException($"'{resourceUri}' does not contain valid resource Id part.", ex);
            }
        }

        public static bool TryParse(string resourceId, out ResourceId parsedId)
        {
            try
            {
                parsedId = new ResourceId(resourceId);
                return true;
            }
            catch
            {
                parsedId = null;
                return false;
            }
        }
    }
}
