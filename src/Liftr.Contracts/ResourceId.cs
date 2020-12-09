//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    /// <summary>
    /// An ARM Resource Identifier (ResourceId) is the unique identifier for a resource or a scope in ARM.
    /// This uniqueness is only enforced at a given point in time, so it is possible for a resource to be
    /// deleted and a new resource created with the same ResourceId.
    ///
    /// ResourceIds are composed of segments, where the first segment is commonly referred to as the 'root scope',
    /// and the last segment is referred to as the 'routing scope'. Segments are separated with '/providers/'.
    ///
    /// Ownership of the resource belongs to the provider in the routing scope, and ARM will use this provider for routing requests.
    ///
    /// https://armwiki.azurewebsites.net/introduction/concepts/resourceids.html
    /// </summary>
    public class ResourceId
    {
        private const string c_subscriptions = "subscriptions";
        private const string c_resourceGroups = "resourceGroups";
        private const string c_providers = "providers";
        private const string c_tenants = "tenants";
        private const string c_scopeDelimiter = "/providers/";
        private const string c_managementGroupResourceIdPrefix = "/providers/Microsoft.Management/managementGroups";

        private readonly string _resourceIdStr;

        public ResourceId(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            _resourceIdStr = resourceId;

            // Parse root scope first
            if (resourceId.OrdinalStartsWith(c_managementGroupResourceIdPrefix))
            {
                // Management Group level
                RootScopeLevel = RootScopeLevel.ManagementGroup;
                var scopeDelimitIndex = resourceId.LastIndexOf(c_scopeDelimiter, StringComparison.OrdinalIgnoreCase);
                if (scopeDelimitIndex == 0)
                {
                    RootScope = resourceId;
                }
                else
                {
                    RootScope = resourceId.Substring(0, resourceId.IndexOf(c_scopeDelimiter, 3, StringComparison.OrdinalIgnoreCase) + 1);
                    RoutingScope = resourceId.Substring(scopeDelimitIndex);
                }

                if (!RootScope.OrdinalEndsWith("/"))
                {
                    RootScope = RootScope + "/";
                }

                var rootScopeParts = RootScope.Split('/');
                if (rootScopeParts.Length != 6)
                {
                    throw new FormatException($"Root scope '{RootScope}' in resourceId '{resourceId}' is management Group level root scope.");
                }

                ManagementGroupName = rootScopeParts[4];
            }
            else
            {
                // Non Management Group level
                if (resourceId.OrdinalContains(c_scopeDelimiter))
                {
                    RootScope = resourceId.Substring(0, resourceId.IndexOf(c_scopeDelimiter, StringComparison.OrdinalIgnoreCase) + 1);
                    RoutingScope = resourceId.Substring(resourceId.LastIndexOf(c_scopeDelimiter, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    RootScope = resourceId;
                }

                if (!RootScope.OrdinalEndsWith("/"))
                {
                    RootScope = RootScope + "/";
                }

                var rootScopeParts = RootScope.Split('/');
                if (resourceId.OrdinalSubstringCount(c_scopeDelimiter) > 1)
                {
                    // {rootScope}/providers/{parentNamespace}/{parentType}/{parentName}/providers/{extensionNamespace}/{extensionType}/{extensionName}
                    RootScopeLevel = RootScopeLevel.Extension;
                }
                else if (RootScope.OrdinalEquals("/"))
                {
                    // '/'
                    RootScopeLevel = RootScopeLevel.Tenant;
                }
                else if (rootScopeParts.Length == 4 && rootScopeParts[1].OrdinalEquals(c_tenants))
                {
                    // '/tenants/{tenantId}/'
                    RootScopeLevel = RootScopeLevel.Tenant;
                }
                else if (rootScopeParts.Length == 4 && rootScopeParts[1].OrdinalEquals(c_subscriptions))
                {
                    // '/subscriptions/{subscriptionId}/'
                    RootScopeLevel = RootScopeLevel.Subscription;
                }
                else if (rootScopeParts.Length == 6 && rootScopeParts[1].OrdinalEquals(c_subscriptions) && rootScopeParts[3].OrdinalEquals(c_resourceGroups))
                {
                    // '/subscriptions/{subscriptionId}/resourceGroups/{groupName}/'
                    RootScopeLevel = RootScopeLevel.ResourceGroup;
                }
                else
                {
                    throw new FormatException($"Root scope '{RootScope}' in resourceId '{resourceId}' is invalid root scope.");
                }

                if (rootScopeParts.Length >= 4 && rootScopeParts[1].OrdinalEquals(c_tenants))
                {
                    // '/tenants/{tenantId}/'
                    TenantId = rootScopeParts[2];
                }

                if (rootScopeParts.Length >= 4 && rootScopeParts[1].OrdinalEquals(c_subscriptions))
                {
                    // '/subscriptions/{subscriptionId}/'
                    SubscriptionId = rootScopeParts[2];
                }

                if (rootScopeParts.Length >= 6 && rootScopeParts[1].OrdinalEquals(c_subscriptions) && rootScopeParts[3].OrdinalEquals(c_resourceGroups))
                {
                    // '/subscriptions/{subscriptionId}/resourceGroups/{groupName}/'
                    ResourceGroup = rootScopeParts[4];
                }
            }

            // Parse routing scope
            if (HasRoutingScope)
            {
                // '/providers/{providerNamespace}
                // '/providers/{providerNamespace}/{type}/{name}'
                // '/providers/{providerNamespace}/{type}/{name}/{childType}/{childName}'
                var routingParts = RoutingScope.Split('/');
                if (routingParts.Length < 3)
                {
                    throw new FormatException($"In resourceId '{resourceId}' the routing parts '{RoutingScope}' are too less.");
                }

                if (routingParts.Length % 2 == 0)
                {
                    throw new FormatException($"In resourceId '{resourceId}' the routing parts '{RoutingScope}' is not in pairs.");
                }

                Provider = routingParts[2];

                if (routingParts.Length >= 5)
                {
                    ResourceType = routingParts[3];
                    ResourceName = routingParts[4];

                    List<ResourceTypeNamePair> names = new List<ResourceTypeNamePair>();
                    names.Add(new ResourceTypeNamePair() { ResourceType = ResourceType, ResourceName = ResourceName });
                    for (int i = 5; i + 1 < routingParts.Length; i += 2)
                    {
                        names.Add(new ResourceTypeNamePair() { ResourceType = routingParts[i], ResourceName = routingParts[i + 1] });
                    }

                    TypedNames = names.ToArray();
                }
            }
        }

        public ResourceId(
            string subscriptionId,
            string resourceGroup,
            string provider,
            string resourceType,
            string resourceName = null,
            string childResourceType = null,
            string childResourceName = null,
            string grandChildResourceType = null,
            string grandChildResourceName = null)
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
                names.Add(new ResourceTypeNamePair() { ResourceType = childResourceType, ResourceName = childResourceName });
                _resourceIdStr = _resourceIdStr + $"/{childResourceType}/{childResourceName}";
            }

            if (!string.IsNullOrEmpty(grandChildResourceType) && !string.IsNullOrEmpty(grandChildResourceName))
            {
                names.Add(new ResourceTypeNamePair() { ResourceType = grandChildResourceType, ResourceName = grandChildResourceName });
                _resourceIdStr = _resourceIdStr + $"/{grandChildResourceType}/{grandChildResourceName}";
            }

            TypedNames = names.ToArray();
        }

        public bool HasRoutingScope => !string.IsNullOrEmpty(RoutingScope);

        /// <summary>
        /// If this is an extension root scope, this is the first root scope.
        /// </summary>
        public string RootScope { get; }

        public string RoutingScope { get; }

        public RootScopeLevel RootScopeLevel { get; }

        public string ManagementGroupName { get; }

        public string TenantId { get; }

        public string SubscriptionId { get; }

        public string ResourceGroup { get; }

        public string Provider { get; }

        public string ResourceType { get; }

        public string ResourceName { get; }

        public string ChildResourceType
        {
            get
            {
                if (TypedNames == null || TypedNames.Length < 2)
                {
                    return null;
                }

                return TypedNames[1].ResourceType;
            }
        }

        public string ChildResourceName
        {
            get
            {
                if (TypedNames == null || TypedNames.Length < 2)
                {
                    return null;
                }

                return TypedNames[1].ResourceName;
            }
        }

        public string GrandChildResourceType
        {
            get
            {
                if (TypedNames == null || TypedNames.Length < 3)
                {
                    return null;
                }

                return TypedNames[2].ResourceType;
            }
        }

        public string GrandChildResourceName
        {
            get
            {
                if (TypedNames == null || TypedNames.Length < 3)
                {
                    return null;
                }

                return TypedNames[2].ResourceName;
            }
        }

        public ResourceTypeNamePair[] TypedNames { get; }

        public override string ToString() => _resourceIdStr;

        /// <summary>
        /// Parse the resource Id in Azure portal Uri. This only support the resource Id start with '/subscriptions/'
        /// </summary>
        public static ResourceId FromResourceUri(string resourceUri)
        {
            try
            {
                var resourceId = resourceUri?.Substring(resourceUri.OrdinalIndexOf($"/{c_subscriptions}/")) ?? throw new ArgumentNullException(nameof(resourceUri));
                return new ResourceId(resourceId);
            }
            catch (Exception ex)
            {
                throw new FormatException($"'{resourceUri}' does not contain '{c_subscriptions}' for starting parsing the Uri.", ex);
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
