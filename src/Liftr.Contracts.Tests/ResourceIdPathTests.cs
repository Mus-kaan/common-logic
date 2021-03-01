//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.Contracts.Tests
{
    public class ResourceIdPathTests
    {
        [Fact]
        public void CanParseResourceIdPath()
        {
            string rid = "/subscriptions/f9d7ebed-adbd-4cb4-b973-aaf82c136138/resourceGroups/limgurg/providers/Microsoft.Datadog/monitors/feng642020";
            string path = $"{rid}/ResourceCreationValidate";
            var success = ResourceIdPath.TryParse(path, out var parsed);
            Assert.True(success);

            Assert.Equal(rid, parsed.ResourceId.ToString());
            Assert.Equal(path, parsed.Path);
            Assert.Equal("MONITORS/RESOURCECREATIONVALIDATE", parsed.TargetResourceType);
            Assert.Equal("/subscriptions/<subscriptionId>/resourceGroups/<resourceGroup>/providers/Microsoft.Datadog/monitors/<name>/ResourceCreationValidate", parsed.GenericPath);
        }

        [Fact]
        public void CanParseResourceId()
        {
            string rid = "/subscriptions/f9d7ebed-adbd-4cb4-b973-aaf82c136138/resourceGroups/limgurg/providers/Microsoft.Datadog/monitors/feng642020";
            var success = ResourceIdPath.TryParse(rid, out var parsed);
            Assert.True(success);

            Assert.Equal(rid, parsed.ResourceId.ToString());
            Assert.Equal(rid, parsed.Path);
            Assert.Equal("MONITORS", parsed.TargetResourceType);
            Assert.Equal("/subscriptions/<subscriptionId>/resourceGroups/<resourceGroup>/providers/Microsoft.Datadog/monitors/<name>", parsed.GenericPath);
        }

        [Fact]
        public void CanParseResourceIdPathWithChild()
        {
            string rid = "/subscriptions/f9d7ebed-adbd-4cb4-b973-aaf82c136138/resourceGroups/limgurg/providers/Microsoft.Datadog/monitors/feng642020/childType/childName1";
            string path = $"{rid}/ResourceCreationValidate";
            var success = ResourceIdPath.TryParse(path, out var parsed);
            Assert.True(success);

            Assert.Equal(rid, parsed.ResourceId.ToString());
            Assert.Equal(path, parsed.Path);
            Assert.Equal("MONITORS/CHILDTYPE/RESOURCECREATIONVALIDATE", parsed.TargetResourceType);
            Assert.Equal("/subscriptions/<subscriptionId>/resourceGroups/<resourceGroup>/providers/Microsoft.Datadog/monitors/<name>/childType/<childName>/ResourceCreationValidate", parsed.GenericPath);
        }

        [Fact]
        public void CanParseResourceIdWithChild()
        {
            string rid = "/subscriptions/f9d7ebed-adbd-4cb4-b973-aaf82c136138/resourceGroups/limgurg/providers/Microsoft.Datadog/monitors/feng642020/childType/childName1";
            var success = ResourceIdPath.TryParse(rid, out var parsed);
            Assert.True(success);

            Assert.Equal(rid, parsed.ResourceId.ToString());
            Assert.Equal(rid, parsed.Path);
            Assert.Equal("MONITORS/CHILDTYPE", parsed.TargetResourceType);
            Assert.Equal("/subscriptions/<subscriptionId>/resourceGroups/<resourceGroup>/providers/Microsoft.Datadog/monitors/<name>/childType/<childName>", parsed.GenericPath);
        }

        [Theory]
        [InlineData("asdasasfdsf")]
        [InlineData("/api/liveness-probe")]
        [InlineData("/api/operationList")]
        [InlineData("/subscriptions1/f9d7ebed-adbd-4cb4-b973-aaf82c136138/resourceGroups/limgurg/providers/Microsoft.Datadog/monitors/feng642020")]
        [InlineData("/subscriptions/f9d7ebed-adbd-4cb4-b973-aaf82c136138/resourceGroups2/limgurg/providers/Microsoft.Datadog/monitors/feng642020")]
        [InlineData("/subscriptions/f9d7ebed-adbd-4cb4-b973-aaf82c136138/resourceGroups/limgurg/providers3/Microsoft.Datadog/monitors/feng642020")]
        public void CanSkipNormalPath(string path)
        {
            Assert.False(ResourceIdPath.TryParse(path, out _));
        }
    }
}
