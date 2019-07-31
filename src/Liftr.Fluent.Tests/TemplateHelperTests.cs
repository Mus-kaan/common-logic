//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Provisioning;
using Xunit;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class TemplateHelperTests
    {
        [Fact]
        public void AssemblePLSTemplate()
        {
            var json = TemplateHelper.GeneratePrivateLinkServiceTemplate(Region.UKSouth, "plsName", "net-rid", "frontend-rid");
        }
    }
}
