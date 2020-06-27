//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr
{
    public sealed class JenkinsOnlyAttribute : FactAttribute
    {
        public JenkinsOnlyAttribute()
        {
            // Local debug will not skip the unit test.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                return;
            }

            if (!TestConstants.IsJenkins())
            {
                Skip = "Ignored for NON Jenkins environments.";
            }
        }
    }
}
