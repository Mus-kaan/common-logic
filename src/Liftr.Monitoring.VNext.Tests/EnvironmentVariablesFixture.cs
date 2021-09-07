//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Liftr.Monitoring.VNext.Tests
{
    // Use to set environment variables needed for local testing
    public class EnvironmentVariablesFixture
    {
        public EnvironmentVariablesFixture()
        {
            Environment.SetEnvironmentVariable("LIFTR_UNIT_TEST_AUTH_FILE_BASE64", string.Empty);
        }
    }
}
