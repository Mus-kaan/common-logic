//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Utilities;

namespace Microsoft.Liftr.Logging.AspNetCore
{
    /// <summary>
    /// This class is a reduced version of <see cref="MetaInfo"/>.
    /// It removed some information that might be sensitive.
    /// </summary>
    public class LivenessPingResult
    {
        public string AssemblyName { get; set; }

        public string Version { get; set; }

        public string LiftrLibraryVersion { get; set; }

        public string Timestamp { get; set; }

        public string OutboundIP { get; set; }

        public ReducedInstanceMetadata InstanceMeta { get; set; }

        internal static LivenessPingResult FromMetaInfo(MetaInfo metaInfo)
        {
            if (metaInfo == null)
            {
                return null;
            }

            var result = new LivenessPingResult()
            {
                AssemblyName = metaInfo.AssemblyName,
                Version = metaInfo.Version,
                LiftrLibraryVersion = metaInfo.LiftrLibraryVersion,
                Timestamp = metaInfo.Timestamp,
                OutboundIP = metaInfo.OutboundIP,
            };

            result.InstanceMeta = ReducedInstanceMetadata.FromInstanceMetadata(metaInfo.InstanceMeta);

            return result;
        }
    }

    public class ReducedInstanceMetadata
    {
        public string MachineNameEnv { get; private set; }

        public ReducedComputeMetadata Compute { get; private set; }

        internal static ReducedInstanceMetadata FromInstanceMetadata(InstanceMetadata instanceMeta)
        {
            if (instanceMeta == null)
            {
                return null;
            }

            var result = new ReducedInstanceMetadata()
            {
                MachineNameEnv = instanceMeta.MachineNameEnv,
            };

            result.Compute = ReducedComputeMetadata.FromComputeMetadata(instanceMeta.Compute);

            return result;
        }
    }

    public class ReducedComputeMetadata
    {
        public string Location { get; private set; }

        public string Name { get; private set; }

        public string VmScaleSetName { get; private set; }

        public string Zone { get; private set; }

        internal static ReducedComputeMetadata FromComputeMetadata(ComputeMetadata compute)
        {
            if (compute == null)
            {
                return null;
            }

            var result = new ReducedComputeMetadata()
            {
                Location = compute.Location,
                Name = compute.Name,
                VmScaleSetName = compute.VmScaleSetName,
                Zone = compute.Zone,
            };

            return result;
        }
    }
}
