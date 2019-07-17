//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Liftr.Fluent.Tests
{
    public static class TestCommon
    {
        public static readonly Region Location = Region.USCentral;

        public static readonly Dictionary<string, string> Tags
            = new Dictionary<string, string>()
            {
                ["Creator"] = "UnitTest",
                ["CreatedAt"] = DateTime.UtcNow.ToShortDateString(),
                ["TestOwner"] = "John Doe",
                ["TestDepartment"] = "IT",
            };

        public static void CheckCommonTags(IDictionary<string, string> tags)
        {
            foreach (var kvp in Tags)
            {
                Assert.Equal(kvp.Value, tags[kvp.Key]);
            }
        }

        public static void AddCommonTags(IDictionary<string, string> tags)
        {
            foreach (var kvp in Tags)
            {
                tags[kvp.Key] = kvp.Value;
            }
        }
    }
}
