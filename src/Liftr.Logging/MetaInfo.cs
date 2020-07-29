//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Logging
{
    public class MetaInfo
    {
        public string AssemblyName { get; set; }

        public string Version { get; set; }

        public string LiftrLibraryVersion { get; set; }

        public InstanceMetadata InstanceMeta { get; set; }

        public string Timestamp { get; set; }
    }
}
