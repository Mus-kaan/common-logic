//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Xunit;

namespace Microsoft.Liftr.Hosting.Tests
{
    public class PrefixKeyVaultSecretManagerTests
    {
        [Fact]
        public void ItWillThrowForInvalidParameter()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var _ = new PrefixKeyVaultSecretManager(string.Empty);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                var _ = new PrefixKeyVaultSecretManager("SomeName-withdash");
            });

            // Make sure it does not throw.
            var validator = new PrefixKeyVaultSecretManager("ValidPrefix");
        }
    }
}
