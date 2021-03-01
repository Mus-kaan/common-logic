//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;

namespace Microsoft.Liftr.Contracts
{
    /// <summary>
    /// an URI path start with a resource Id.
    /// </summary>
    public sealed class ResourceIdPath
    {
        private ResourceIdPath()
        {
        }

        public ResourceId ResourceId { get; private set; }

        public string Path { get; private set; }

        /// <summary>
        /// The particular part is switched to place holder.
        /// </summary>
        public string GenericPath { get; private set; }

        public string TargetResourceType { get; private set; }

        public static bool TryParse(string path, out ResourceIdPath resourceIdPath)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            resourceIdPath = null;
            var parts = path.Split('/');
            if (parts.Length < 7)
            {
                return false;
            }

            string actionPart = null;
            if (parts.Length % 2 == 0)
            {
                // there is an dangling 'action' part.
                // e.g. 'validation' in '/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/liftr-acr-rg/providers/Microsoft.ContainerRegistry/registries/liftrmsacr/validation'
                actionPart = parts.Last();
                parts = parts.Take(parts.Length - 1).ToArray();
            }

            ResourceIdPath parsed = new ResourceIdPath() { Path = path };
            try
            {
                var resourceIdPart = string.Join("/", parts);
                var rid = new ResourceId(resourceIdPart);
                parsed.ResourceId = rid;

                string childResourceType = null;
                string childResourceName = null;

                if (!string.IsNullOrEmpty(rid.ChildResourceType) && !string.IsNullOrEmpty(rid.ChildResourceName))
                {
                    childResourceType = rid.ChildResourceType;
                    childResourceName = "<childName>";
                }

                var genericResourceId = new ResourceId(
                    "<subscriptionId>",
                    "<resourceGroup>",
                    rid.Provider,
                    rid.ResourceType,
                    string.IsNullOrEmpty(rid.ResourceName) ? null : "<name>",
                    childResourceType,
                    childResourceName);

                parsed.GenericPath = string.IsNullOrEmpty(actionPart) ? genericResourceId.ToString() : $"{genericResourceId}/{actionPart}";
                parsed.TargetResourceType = string.IsNullOrEmpty(actionPart) ? rid.TargetResourceType : $"{rid.TargetResourceType}/{actionPart}".ToUpperInvariant();

                resourceIdPath = parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
