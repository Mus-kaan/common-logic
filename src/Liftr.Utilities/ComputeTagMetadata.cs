//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Utilities
{
    public class ComputeTagMetadata
    {
        public string BlobEndpoint { get; set; }

        public string VaultEndpoint { get; set; }

        public string ASPNETCORE_ENVIRONMENT { get; set; }

        public string DOTNET_ENVIRONMENT { get; set; }

        public string GCS_REGION { get; set; }

        public IDictionary<string, string> Tags { get; set; }
    }
}
