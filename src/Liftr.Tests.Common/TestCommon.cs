//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
    public static class TestCommon
    {
        public static readonly Region Location = Region.USEast;

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
                if (!kvp.Value.StrictEquals(tags[kvp.Key]))
                {
                    throw new InvalidOperationException($"Tags value not equal. Expect: {kvp.Value}, Actual: {tags[kvp.Key]}");
                }
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
