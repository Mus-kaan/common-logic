//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts
{
    public class CosmosDBConnectionStrings
    {
        public string PrimaryConnectionString { get; set; }

        public string SecondaryConnectionString { get; set; }

        public string PrimaryReadOnlyConnectionString { get; set; }

        public string SecondaryReadOnlyConnectionString { get; set; }
    }
}
