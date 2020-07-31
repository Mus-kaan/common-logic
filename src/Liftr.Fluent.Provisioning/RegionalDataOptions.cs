//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class RegionalDataOptions
    {
        public string ActiveDBKeyName { get; set; }

        public string SecretPrefix { get; set; }

        public string DomainName { get; set; }

        public string DNSZoneId { get; set; }

        public string LogAnalyticsWorkspaceId { get; set; }

        public string GlobalKeyVaultResourceId { get; set; }

        public string GlobalStorageResourceId { get; set; }

        public Dictionary<string, string> OneCertCertificates { get; set; } = new Dictionary<string, string>();

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }

        public int DataPlaneStorageCountPerSubscription { get; set; }

        public bool EnableVNet { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(ActiveDBKeyName))
            {
                throw new InvalidOperationException($"{nameof(ActiveDBKeyName)} should not be null.");
            }

            if (string.IsNullOrEmpty(DomainName))
            {
                throw new InvalidOperationException($"{nameof(DomainName)} should not be null.");
            }

            if (string.IsNullOrEmpty(DNSZoneId))
            {
                throw new InvalidOperationException($"{nameof(DNSZoneId)} should not be null.");
            }

            if (string.IsNullOrEmpty(SecretPrefix))
            {
                throw new InvalidOperationException($"{nameof(SecretPrefix)} should not be null.");
            }

            if (string.IsNullOrEmpty(LogAnalyticsWorkspaceId))
            {
                throw new InvalidOperationException($"{nameof(LogAnalyticsWorkspaceId)} should not be null.");
            }

            if (DataPlaneStorageCountPerSubscription < 0)
            {
                throw new InvalidOperationException($"{DataPlaneStorageCountPerSubscription} should not be non-negative.");
            }

            if (DataPlaneStorageCountPerSubscription > 0)
            {
                if (DataPlaneSubscriptions == null || !DataPlaneSubscriptions.Any())
                {
                    throw new InvalidOperationException("data plane Subscriptions cannot be empty.");
                }
            }

            if (ActiveDBKeyName.OrdinalEquals("Primary MongoDB Connection String")
            || ActiveDBKeyName.OrdinalEquals("Secondary MongoDB Connection String")
            || ActiveDBKeyName.OrdinalEquals("Primary Read-Only MongoDB Connection String")
            || ActiveDBKeyName.OrdinalEquals("Secondary Read-Only MongoDB Connection String"))
            {
                return;
            }
            else
            {
                throw new InvalidOperationException($"{nameof(ActiveDBKeyName)} must be one of the following: 'Primary MongoDB Connection String', 'Secondary MongoDB Connection String', 'Primary Read-Only MongoDB Connection String', 'Secondary Read-Only MongoDB Connection String'.");
            }
        }
    }
}
