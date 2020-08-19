﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class GenevaOptions
    {
        public string MONITORING_GCS_ENVIRONMENT { get; set; }

        public string MONITORING_GCS_ACCOUNT { get; set; }

        public string MONITORING_GCS_NAMESPACE { get; set; }

        public string MONITORING_CONFIG_VERSION { get; set; }

        public string MDM_ACCOUNT { get; set; }

        public string MDM_NAMESPACE { get; set; }

        public string MDM_ENDPOINT { get; set; } = "https://global.metrics.nsatc.net/";

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(MONITORING_GCS_ENVIRONMENT))
            {
                throw new InvalidHostingOptionException($"{nameof(MONITORING_GCS_ENVIRONMENT)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(MONITORING_GCS_ACCOUNT))
            {
                throw new InvalidHostingOptionException($"{nameof(MONITORING_GCS_ACCOUNT)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(MONITORING_GCS_NAMESPACE))
            {
                throw new InvalidHostingOptionException($"{nameof(MONITORING_GCS_NAMESPACE)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(MONITORING_CONFIG_VERSION))
            {
                throw new InvalidHostingOptionException($"{nameof(MONITORING_CONFIG_VERSION)} cannot be null or empty.");
            }
        }
    }
}
