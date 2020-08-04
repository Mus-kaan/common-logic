﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.Liftr
{
    public static class TestCommon
    {
        public static readonly Region Location = Region.USEast;
        public static readonly int EventHubPartitionCount = 32;
        public static readonly List<string> EventHubConsumerGroups = new List<string> { "cgroup1", "cgroup2" };

        public static readonly Dictionary<string, string> Tags
            = new Dictionary<string, string>()
            {
                ["Creator"] = "UnitTest",
                ["CreatedAt"] = DateTime.UtcNow.ToShortDateString(),
                ["TestRunningMachine"] = Environment.MachineName,
                ["TestDummyTag"] = "Dummy value",
                ["ResourceCreationTimestamp"] = DateTime.UtcNow.ToZuluString(),
            };

        public static void CheckCommonTags(IDictionary<string, string> tags)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            foreach (var kvp in Tags)
            {
                if (kvp.Key.OrdinalEquals("ResourceCreationTimestamp"))
                {
                    // ignore the Tag in Zulu time format. It will be converted to not Zulu format by ARM.
                    continue;
                }

                if (!kvp.Value.StrictEquals(tags[kvp.Key]))
                {
                    throw new InvalidOperationException($"Tags value not equal. Expect: {kvp.Value}, Actual: {tags[kvp.Key]}");
                }
            }
        }

        public static void AddCommonTags(IDictionary<string, string> tags, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            foreach (var kvp in Tags)
            {
                tags[kvp.Key] = kvp.Value;
            }

            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(memberName))
            {
                return;
            }

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.OrdinalContains("test"))
            {
                tags["TestFile"] = fileName;
                tags["TestMethod"] = memberName;
            }
        }
    }
}
