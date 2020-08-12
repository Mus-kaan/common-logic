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

                if (parsedTags.ContainsKey("ENV_" + nameof(result.ASPNETCORE_ENVIRONMENT)))
                {
                    result.ASPNETCORE_ENVIRONMENT = parsedTags["ENV_" + nameof(result.ASPNETCORE_ENVIRONMENT)];
                }

                if (parsedTags.ContainsKey("ENV_" + nameof(result.DOTNET_ENVIRONMENT)))
                {
                    result.DOTNET_ENVIRONMENT = parsedTags["ENV_" + nameof(result.DOTNET_ENVIRONMENT)];
                }

                if (parsedTags.ContainsKey("ENV_" + nameof(result.VaultEndpoint)))
                {
                    result.VaultEndpoint = parsedTags["ENV_" + nameof(result.VaultEndpoint)];
                }

                if (parsedTags.ContainsKey("ENV_" + nameof(result.GCS_REGION)))
                {
                    result.VaultEndpoint = parsedTags["ENV_" + nameof(result.GCS_REGION)];
                }

                return result;
            }

            return null;
        }
    }
}
