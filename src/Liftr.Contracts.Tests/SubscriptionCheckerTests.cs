//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Liftr.Contracts.Tests
{
    public class SubscriptionCheckerTests
    {
        [Fact]
        public void BasicUsage()
        {
            var guid1 = Guid.NewGuid().ToString();
            var guid2 = Guid.NewGuid().ToString();
            var guid3 = Guid.NewGuid().ToString();

            var options = new SubscriptionCheckerOptions()
            {
                Subscriptions = new List<string>() { guid1, guid2, guid3 },
            };

            var hack = new SubscriptionChecker(options);

            Assert.True(hack.Contains(guid1));
            Assert.True(hack.Contains(guid2));
            Assert.True(hack.Contains(guid3));

            Assert.True(hack.Contains(guid1.ToLowerInvariant()));
            Assert.True(hack.Contains(guid2.ToUpperInvariant()));
            Assert.True(hack.Contains(guid3));

            Assert.False(hack.Contains(Guid.NewGuid().ToString()));
            Assert.False(hack.Contains(Guid.NewGuid().ToString()));
            Assert.False(hack.Contains(Guid.NewGuid().ToString()));

            hack = new SubscriptionChecker(new SubscriptionCheckerOptions());
            Assert.False(hack.Contains(guid1));
            Assert.False(hack.Contains(guid2));
            Assert.False(hack.Contains(guid3));

            Assert.Throws<InvalidOperationException>(() =>
            {
                options = new SubscriptionCheckerOptions()
                {
                    Subscriptions = new List<string>() { "asdasd", guid2, guid3 },
                };

                var hack = new SubscriptionChecker(options);
            });
        }
    }
}
