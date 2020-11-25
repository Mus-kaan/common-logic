//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Liftr
{
    public sealed class CheckInValidationAttribute : FactAttribute
    {
        public CheckInValidationAttribute(bool skipLinux = false)
        {
            // Local debug will not skip the unit test.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                return;
            }

            SkipLinux = skipLinux;

            if (skipLinux && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Skip = "Ignored for Linux builds";
            }
            else if (!IsCheckInValidation())
            {
                Skip = "Ignored for non check in validation builds";
            }
        }

        public bool SkipLinux { get; }

        public static bool IsCheckInValidation()
        {
            var testType = Environment.GetEnvironmentVariable("LIFTR_TEST_TYPE");
            return testType?.OrdinalEquals("CHECK_IN_TEST") == true;
        }
    }
}
