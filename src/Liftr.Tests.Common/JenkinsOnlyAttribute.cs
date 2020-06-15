//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Xunit;

namespace Microsoft.Liftr
{
    public sealed class JenkinsOnlyAttribute : FactAttribute
    {
        public const string LIFTR_ENV = nameof(LIFTR_ENV);
        public const string JENKINS = nameof(JENKINS);

        public JenkinsOnlyAttribute()
        {
            // Local debug will not skip the unit test.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                return;
            }

#pragma warning disable CS0162 // Unreachable code detected
            if (!IsJenkins())
#pragma warning restore CS0162 // Unreachable code detected
            {
                Skip = "Ignored for NON Jenkins environments.";
            }
        }

        public static bool IsJenkins()
        {
            var envType = Environment.GetEnvironmentVariable(LIFTR_ENV);
            return envType?.OrdinalEquals(JENKINS) == true;
        }
    }
}
