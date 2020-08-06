//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Utilities
{
    public class MetaInfo
    {
        public string AssemblyName { get; set; }

        public string Version { get; set; }

        public string LiftrLibraryVersion { get; set; }

        public InstanceMetadata InstanceMeta { get; set; }

        public string Timestamp { get; set; }

        public ComputeTagMetadata GetComputeTagMetadata()
        {
            ComputeTagMetadata result = new ComputeTagMetadata();
            if (TagStringParser.TryParse(InstanceMeta?.Compute?.Tags, out var parsedTags))
            {
                result.Tags = parsedTags;

                if (parsedTags.ContainsKey(nameof(result.ASPNETCORE_ENVIRONMENT)))
                {
                    result.ASPNETCORE_ENVIRONMENT = parsedTags[nameof(result.ASPNETCORE_ENVIRONMENT)];
                }

                if (parsedTags.ContainsKey(nameof(result.VaultEndpoint)))
                {
                    result.VaultEndpoint = parsedTags[nameof(result.VaultEndpoint)];
                }

                if (parsedTags.ContainsKey(nameof(result.GCS_REGION)))
                {
                    result.VaultEndpoint = parsedTags[nameof(result.GCS_REGION)];
                }

                return result;
            }

            return null;
        }
    }
}
