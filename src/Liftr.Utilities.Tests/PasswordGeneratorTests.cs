//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.Utilities.Tests
{
    public class PasswordGeneratorTests
    {
        [Theory]
        [InlineData(12)]
        [InlineData(34)]
        public void CanGenerate(int len)
        {
            for (var i = 0; i < 10; i++)
            {
                var password = PasswordGenerator.Generate(len);
                Assert.Equal(len, password.Length);
            }

            for (var i = 0; i < 10; i++)
            {
                var password = PasswordGenerator.Generate(len, includeSpecialCharacter: false);
                Assert.Equal(len, password.Length);

                foreach (var c in password)
                {
                    Assert.False(PasswordGenerator.special.Contains(c, System.StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }
}
