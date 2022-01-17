//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.DBService.Contracts.Interfaces
{
    public interface IDBOptions
    {
        string PrimaryConnectionString { get; set; }

        string SecretKey { get; set; }

        string DatabaseName { get; set; }

        bool LogDBOperation { get; set; }

        /// <summary>
        /// Validate the db options
        /// </summary>
        bool Validate();
    }
}
