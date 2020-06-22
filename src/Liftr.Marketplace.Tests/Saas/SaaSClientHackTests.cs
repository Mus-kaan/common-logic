//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace;
using System;
using System.Collections.Generic;
using Xunit;

namespace Liftr.Marketplace.Tests.Saas
{
    public class SaaSClientHackTests
    {
        [Fact]
        public void BasicUsage()
        {
            var guid1 = Guid.NewGuid().ToString();
            var guid2 = Guid.NewGuid().ToString();
            var guid3 = Guid.NewGuid().ToString();

            var options = new SaaSClientHackOptions()
            {
                IgnoringSubscriptions = new List<string>() { guid1, guid2, guid3 },
            };

            var hack = new SaaSClientHack(options);

            Assert.True(hack.ShouldIgnoreSaaSCreateFailure(guid1));
            Assert.True(hack.ShouldIgnoreSaaSCreateFailure(guid2));
            Assert.True(hack.ShouldIgnoreSaaSCreateFailure(guid3));

            Assert.True(hack.ShouldIgnoreSaaSCreateFailure(guid1.ToLowerInvariant()));
            Assert.True(hack.ShouldIgnoreSaaSCreateFailure(guid2.ToUpperInvariant()));
            Assert.True(hack.ShouldIgnoreSaaSCreateFailure(guid3));

            Assert.False(hack.ShouldIgnoreSaaSCreateFailure(Guid.NewGuid().ToString()));
            Assert.False(hack.ShouldIgnoreSaaSCreateFailure(Guid.NewGuid().ToString()));
            Assert.False(hack.ShouldIgnoreSaaSCreateFailure(Guid.NewGuid().ToString()));

            hack = new SaaSClientHack(new SaaSClientHackOptions());
            Assert.False(hack.ShouldIgnoreSaaSCreateFailure(guid1));
            Assert.False(hack.ShouldIgnoreSaaSCreateFailure(guid2));
            Assert.False(hack.ShouldIgnoreSaaSCreateFailure(guid3));

            Assert.Throws<InvalidOperationException>(() =>
            {
                options = new SaaSClientHackOptions()
                {
                    IgnoringSubscriptions = new List<string>() { "asdasd", guid2, guid3 },
                };

                var hack = new SaaSClientHack(options);
            });
        }
    }
}
