//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public static class TestPostgreCredentials
    {
        private const string LIFTR_UNIT_TEST_POSTGRE_ADMIN_USERNAME = nameof(LIFTR_UNIT_TEST_POSTGRE_ADMIN_USERNAME);
        private const string LIFTR_UNIT_TEST_POSTGRE_ADMIN_PASSWORD = nameof(LIFTR_UNIT_TEST_POSTGRE_ADMIN_PASSWORD);

        public static string AdminUsername
        {
            get
            {
                var username = Environment.GetEnvironmentVariable(LIFTR_UNIT_TEST_POSTGRE_ADMIN_USERNAME);
                if (string.IsNullOrEmpty(username))
                {
                    throw new InvalidOperationException($"Cannot find the admin username the environment variable with name {LIFTR_UNIT_TEST_POSTGRE_ADMIN_USERNAME}. Details: https://aka.ms/liftr-test-cred");
                }

                return username;
            }
        }

        public static string AdminPassword
        {
            get
            {
                var username = Environment.GetEnvironmentVariable(LIFTR_UNIT_TEST_POSTGRE_ADMIN_PASSWORD);
                if (string.IsNullOrEmpty(username))
                {
                    throw new InvalidOperationException($"Cannot find the admin password the environment variable with name {LIFTR_UNIT_TEST_POSTGRE_ADMIN_PASSWORD}. Details: https://aka.ms/liftr-test-cred");
                }

                return username;
            }
        }
    }
}
