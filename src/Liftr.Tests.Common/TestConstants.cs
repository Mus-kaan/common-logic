//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr
{
    public static class TestConstants
    {
        public const string LIFTR_ENV = nameof(LIFTR_ENV);

        public const string JENKINS = nameof(JENKINS);

        public static bool IsJenkins()
        {
            var envType = Environment.GetEnvironmentVariable(TestConstants.LIFTR_ENV);
            return envType?.OrdinalEquals(TestConstants.JENKINS) == true;
        }

        public static bool IsNonJenkins()
        {
            if (!System.Diagnostics.Debugger.IsAttached &&
                !TestConstants.IsJenkins())
            {
                return true;
            }

            return false;
        }
    }
}
