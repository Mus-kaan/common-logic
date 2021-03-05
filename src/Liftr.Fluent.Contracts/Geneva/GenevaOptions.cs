//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;

namespace Microsoft.Liftr.Fluent.Contracts.Geneva
{
    public class GenevaOptions
    {
        public string MonitoringTenant { get; set; }

        public string MonitoringRole { get; set; }

        public string MonitoringGCSEnvironment { get; set; }

        public string MonitoringGCSAccount { get; set; }

        public string MonitoringGCSNamespace { get; set; }

        public string MonitoringGCSClientCertificateSAN { get; set; }

        public string MonitoringConfigVersion { get; set; }

        public void CheckValid()
        {
            bool isValidEnv = typeof(MonitoringGcsEnvironments).GetConstantsValues<string>().Any(e => e.StrictEquals(MonitoringGCSEnvironment));
            if (!isValidEnv)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentOutOfRangeException($"{nameof(MonitoringGCSEnvironment)} is not a valid value.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }
        }
    }
}
