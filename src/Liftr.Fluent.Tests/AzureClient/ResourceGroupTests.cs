//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class ResourceGroupTests
    {
        private readonly ITestOutputHelper _output;

        public ResourceGroupTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkipInOfficialBuild]
        public async Task CanCreateAndDeleteGroupAsync()
        {
            using (var scope = new TestResourceGroupScope("unittest-rg-", _output))
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var retrieved = await client.GetResourceGroupAsync(scope.ResourceGroupName);

                TestCommon.CheckCommonTags(retrieved.Inner.Tags);

                await client.DeleteResourceGroupAsync(scope.ResourceGroupName);

                // It is deleted.
                Assert.Null(await client.GetResourceGroupAsync(scope.ResourceGroupName));
            }
        }

        [SkipInOfficialBuild]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task CleanUpOldTestRGAsync()
        {
            using (var scope = new TestResourceGroupScope("unittest-rg-", _output))
            {
                var client = scope.Client;
                try
                {
                    await client.DeleteResourceGroupWithTagAsync("Creator", "UnitTest", (IReadOnlyDictionary<string, string> tags) =>
                    {
                        if (tags.ContainsKey("CreatedAt"))
                        {
                            try
                            {
                                var timeStamp = DateTime.Parse(tags["CreatedAt"], CultureInfo.InvariantCulture);
                                if (timeStamp < DateTime.Now.AddDays(-2))
                                {
                                    return true;
                                }
                            }
                            catch
                            {
                            }
                        }

                        return false;
                    });
                }
                catch
                {
                }
            }
        }
    }
}
