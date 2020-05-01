//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Linq;
using Xunit;

namespace Microsoft.Liftr.Contracts.Tests
{
    public class EnvironmentTypeTests
    {
        private const string c_allShortNames = "prod, euap, df, dev, test, ff, mc";

        [Fact]
        public void VerifyEnvironmentTypeShortNames()
        {
            var allShortNames = string.Join(", ", EnumUtil.GetValues<EnvironmentType>().Select(v => v.ShortName()));
            Assert.Equal(c_allShortNames, allShortNames);
        }
    }
}
