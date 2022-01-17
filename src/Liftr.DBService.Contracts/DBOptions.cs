//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DBService.Contracts.Interfaces;

namespace Microsoft.Liftr.DBService.Contracts
{
    public class DBOptions : IDBOptions
    {
        public string PrimaryConnectionString { get; set; }

        public string SecretKey { get; set; }

        public string DatabaseName { get; set; }

        public bool LogDBOperation { get; set; } = true;

        public bool Validate()
        {
            return true;
        }
    }
}