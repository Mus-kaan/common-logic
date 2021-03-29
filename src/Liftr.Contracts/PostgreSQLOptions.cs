//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts
{
    public class PostgreSQLOptions
    {
        public string Server { get; set; }

        public string ServerResourceName { get; set; }

        public string Database { get; set; } = "postgres";

        public string Username { get; set; } = "adminuser";

        public string Password { get; set; }

        public string ConnectionString => $"Server={Server};Username={Username}@{ServerResourceName};Database={Database};Port=5432;Password={Password};SSLMode=Require";
    }
}
