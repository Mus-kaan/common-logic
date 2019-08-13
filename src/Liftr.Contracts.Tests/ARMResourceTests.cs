//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.ARM;
using System;
using Xunit;

namespace Microsoft.Liftr.Contracts.Tests
{
    public class ARMResourceTests
    {
        [Fact]
        public void SetTypePropertyThrow()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                var armResource = new ARMResource()
                {
                    Id = "id",
                    Location = "location",
                    Name = "name",
                    Tags = null,
                    Type = "type",
                };
            });
        }

        [Fact]
        public void GetTypePropertyThrow()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                var armResource = new ARMResource()
                {
                    Id = "id",
                    Location = "location",
                    Name = "name",
                    Tags = null,
                };

                var type = armResource.Type;
            });
        }
    }
}
