﻿//-----------------------------------------------------------------------------
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
            if (parts.Length >= 9 && parts.Length % 2 == 1)
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
            ResourceType = parts[7];
            ResourceName = parts[8];

            List<ResourceTypeNamePair> names = new List<ResourceTypeNamePair>();
            for (int i = 7; i < parts.Length; i += 2)
            {
                names.Add(new ResourceTypeNamePair() { ResourceType = parts[i], ResourceName = parts[i + 1] });
            }

            TypedNames = names.ToArray();
        }

        public ResourceId(string subscriptionId, string resourceGroup, string provider, string resourceType, string resourceName)
        {
            SubscriptionId = subscriptionId;
            ResourceGroup = resourceGroup;
            Provider = provider;
            ResourceType = resourceType;
            ResourceName = resourceName;
            List<ResourceTypeNamePair> names = new List<ResourceTypeNamePair>();
            names.Add(new ResourceTypeNamePair() { ResourceType = resourceType, ResourceName = resourceName });
            TypedNames = names.ToArray();
            _resourceIdStr = $"/{c_subscriptions}/{SubscriptionId}/{c_resourceGroups}/{ResourceGroup}/{c_providers}/{Provider}/{ResourceType}/{ResourceName}";
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
                var resourceId = resourceUri?.Substring(resourceUri.OrdinalIndexOf($"/{c_subscriptions}/")) ?? throw new ArgumentNullException(nameof(resourceUri));
                return new ResourceId(resourceId);
            }
            catch (Exception ex)
            {
                throw new FormatException($"'{resourceUri}' does not contain valid resource Id part.", ex);
            }
        }
    }
}
