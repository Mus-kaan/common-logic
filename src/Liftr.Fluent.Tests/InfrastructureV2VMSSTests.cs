//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Provisioning;
using Xunit;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class InfrastructureV2VMSSTests
    {
        [Theory]
        [InlineData("/subscriptions/321cef5a-79cf-487f-8cba-95a9f97a4f72/resourceGroups/LFImgAMERG/providers/Microsoft.Compute/galleries/LFameSIG/images/LogForwarderV2SBI/versions/3.2009.1392194", "/subscriptions/321cef5a-79cf-487f-8cba-95a9f97a4f72/resourceGroups/LFImgAMERG/providers/Microsoft.Compute/galleries/LFameSIG/images/LogForwarderV2SBI/versions/3.2009.1392194")]
        [InlineData("/subscriptions/321cef5a-79cf-487f-8cba-95a9f97a4f72/resourceGroups/LFImgAMERG/providers/Microsoft.Compute/galleries/LFameSIG/images/LogForwarderV2SBI/versions/03.2009.1392194", "/subscriptions/321cef5a-79cf-487f-8cba-95a9f97a4f72/resourceGroups/LFImgAMERG/providers/Microsoft.Compute/galleries/LFameSIG/images/LogForwarderV2SBI/versions/3.2009.1392194")]
        [InlineData("/subscriptions/321cef5a-79cf-487f-8cba-95a9f97a4f72/resourceGroups/LFImgAMERG/providers/Microsoft.Compute/galleries/LFameSIG/images/LogForwarderV2SBI/versions/3.02009.1392194", "/subscriptions/321cef5a-79cf-487f-8cba-95a9f97a4f72/resourceGroups/LFImgAMERG/providers/Microsoft.Compute/galleries/LFameSIG/images/LogForwarderV2SBI/versions/3.2009.1392194")]
        [InlineData("/subscriptions/321cef5a-79cf-487f-8cba-95a9f97a4f72/resourceGroups/LFImgAMERG/providers/Microsoft.Compute/galleries/LFameSIG/images/LogForwarderV2SBI/versions/3.2009.01392194", "/subscriptions/321cef5a-79cf-487f-8cba-95a9f97a4f72/resourceGroups/LFImgAMERG/providers/Microsoft.Compute/galleries/LFameSIG/images/LogForwarderV2SBI/versions/3.2009.1392194")]
        public void CanParseIMageVersionId(string original, string expected)
        {
            var parsed = InfrastructureV2.ParseImageVersion(original);
            Assert.Equal(expected, parsed);
        }
    }
}
