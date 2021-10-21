//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Prom2IcM
{
    public class ICMClientOptions
    {
        public string ICMConnectorEndpoint { get; set; } = "https://prod.microsofticm.com/Connector3/ConnectorIncidentManager.svc";

        public string KeyVaultEndpoint { get; set; }

        public string IcmConnectorCertificateName { get; set; } = "icm-cert";

        public string IcmConnectorId { get; set; }

        public string IcmRoutingId { get; set; } = "prometheus://Liftr/prom2icm";

        public string NotificationEmail { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(ICMConnectorEndpoint))
            {
                throw new ArgumentException($"'{nameof(ICMConnectorEndpoint)}' cannot be empty. Please set it in environment varialbe '{nameof(ICMClientOptions)}__{nameof(ICMConnectorEndpoint)}'.");
            }

            if (string.IsNullOrEmpty(KeyVaultEndpoint))
            {
                throw new ArgumentException($"'{nameof(KeyVaultEndpoint)}' cannot be empty. Please set it in environment varialbe '{nameof(ICMClientOptions)}__{nameof(KeyVaultEndpoint)}'.");
            }

            if (string.IsNullOrEmpty(IcmConnectorCertificateName))
            {
                throw new ArgumentException($"'{nameof(IcmConnectorCertificateName)}' cannot be empty. Please set it in environment varialbe '{nameof(ICMClientOptions)}__{nameof(IcmConnectorCertificateName)}'.");
            }

            if (!Guid.TryParse(IcmConnectorId, out var connectorId))
            {
                throw new ArgumentException($"'{nameof(IcmConnectorId)}' is invalid Guid. Please set it in environment varialbe '{nameof(ICMClientOptions)}__{nameof(IcmConnectorId)}'.");
            }

            if (string.IsNullOrEmpty(NotificationEmail))
            {
                throw new ArgumentException($"'{nameof(NotificationEmail)}' cannot be empty. Please set it in environment varialbe '{nameof(ICMClientOptions)}__{nameof(NotificationEmail)}'.");
            }
        }
    }
}
