//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Liftr
{
    public sealed class SkipInOfficialBuildTheoryAttribute : TheoryAttribute
    {
        public SkipInOfficialBuildTheoryAttribute(bool skipLinux = false)
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
            else if (IsOfficialBuild())
            {
                Skip = "Ignored for offical builds";
            }
        }

        public bool SkipLinux { get; }

        private static bool IsOfficialBuild()
        {
            var buildType = Environment.GetEnvironmentVariable("CDP_BUILD_TYPE");
            return buildType?.OrdinalEquals("Official") == true;
        }
    }
}
