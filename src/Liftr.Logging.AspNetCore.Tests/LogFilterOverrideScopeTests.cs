//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog.Events;
using Xunit;

namespace Microsoft.Liftr.Logging.AspNetCore.Tests
{
    public class LogFilterOverrideScopeTests
    {
        [Fact]
        public void EmptyContructorWillNotThrow()
        {
            using (new LogFilterOverrideScope())
            {
            }
        }

        [Fact]
        public void NotEnabledWillNotThrow()
        {
            using (new LogFilterOverrideScope(LogEventLevel.Error))
            {
                using (new LogFilterOverrideScope(LogEventLevel.Information))
                {
                }
            }
        }

        [Fact]
        public void LowerFilterWillWin()
        {
            var levelSwitch = LogFilterOverrideScope.EnableFilterOverride(LogEventLevel.Error);
            Assert.Equal(LogEventLevel.Error, levelSwitch.MinimumLevel);

            using (new LogFilterOverrideScope(LogEventLevel.Warning))
            {
                Assert.Equal(LogEventLevel.Warning, levelSwitch.MinimumLevel);

                using (new LogFilterOverrideScope(LogEventLevel.Information))
                {
                    Assert.Equal(LogEventLevel.Information, levelSwitch.MinimumLevel);
                }

                // Due to the outter scope, the lower inner scope is still working.
                Assert.Equal(LogEventLevel.Information, levelSwitch.MinimumLevel);
            }

            // Back to default.
            Assert.Equal(LogEventLevel.Error, levelSwitch.MinimumLevel);
        }
    }
}
