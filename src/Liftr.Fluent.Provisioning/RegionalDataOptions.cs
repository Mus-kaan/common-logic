//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class RegionalDataOptions
    {
        public string SecretPrefix { get; set; }

        public string DomainName { get; set; }

        public string DNSZoneId { get; set; }

        public string LogAnalyticsWorkspaceId { get; set; }

        public string GlobalKeyVaultResourceId { get; set; }

        public string GlobalStorageResourceId { get; set; }

        public string GlobalCosmosDBResourceId { get; set; }

        public string GlobalTrafficManagerResourceId { get; set; }

        public Dictionary<string, string> OneCertCertificates { get; set; } = new Dictionary<string, string>();

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }

        public int DataPlaneStorageCountPerSubscription { get; set; }

        public IEnumerable<string> OutboundIPList { get; set; }

        public bool EnableVNet { get; set; }

        public bool EnableThanos { get; set; }

        public bool DBSupport { get; set; } = true;

        public bool? CreateDBWithZoneRedundancy { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(DomainName))
            {
                throw new InvalidHostingOptionException($"{nameof(DomainName)} should not be null.");
            }

            if (string.IsNullOrEmpty(DNSZoneId))
            {
                throw new InvalidHostingOptionException($"{nameof(DNSZoneId)} should not be null.");
            }

            if (string.IsNullOrEmpty(SecretPrefix))
            {
                throw new InvalidHostingOptionException($"{nameof(SecretPrefix)} should not be null.");
            }

            if (string.IsNullOrEmpty(GlobalTrafficManagerResourceId))
            {
                throw new InvalidHostingOptionException($"{nameof(GlobalTrafficManagerResourceId)} should not be null.");
            }

            if (string.IsNullOrEmpty(LogAnalyticsWorkspaceId))
            {
                throw new InvalidHostingOptionException($"{nameof(LogAnalyticsWorkspaceId)} should not be null.");
            }

            if (DataPlaneStorageCountPerSubscription < 0)
            {
                throw new InvalidHostingOptionException($"{DataPlaneStorageCountPerSubscription} should not be non-negative.");
            }

            if (DataPlaneStorageCountPerSubscription > 0)
            {
                if (DataPlaneSubscriptions == null || !DataPlaneSubscriptions.Any())
                {
                    throw new InvalidHostingOptionException("data plane Subscriptions cannot be empty.");
                }
            }
        }
    }
}
