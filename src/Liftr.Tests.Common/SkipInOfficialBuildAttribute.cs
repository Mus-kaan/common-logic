//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Xunit;

namespace Microsoft.Liftr
{
    public sealed class SkipInOfficialBuildAttribute : FactAttribute
    {
        public SkipInOfficialBuildAttribute()
        {
            if (IsOfficialBuild())
            {
                Skip = "Ignored for offical builds";
            }
        }

        private static bool IsOfficialBuild()
        {
            var buildType = Environment.GetEnvironmentVariable("CDP_BUILD_TYPE");
            return buildType?.OrdinalEquals("Official") == true;
        }
    }
}
