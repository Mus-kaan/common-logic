//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Liftr
{
    public sealed class CheckInValidationTheoryAttribute : TheoryAttribute
    {
        public CheckInValidationTheoryAttribute(bool skipLinux = false)
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
            else if (!CheckInValidationAttribute.IsCheckInValidation())
            {
                Skip = "Ignored for non check in validation builds";
            }
        }

        public bool SkipLinux { get; }
    }
}
