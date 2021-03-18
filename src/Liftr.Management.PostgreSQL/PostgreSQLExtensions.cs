//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Management.PostgreSQL
{
    public static class PostgreSQLExtensions
    {
        public static Task RegisterPostgreSQLRPAsync(this ILiftrAzure liftrAzure)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            return liftrAzure.RegisterResourceProviderAsync("Microsoft.DBforPostgreSQL");
        }
    }
}
