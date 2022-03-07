//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.DBService.Contracts.Tests
{
    public class EntityUpdateAttributeTests
    {
        [Fact]
        public void Validate()
        {
            bool allowed = true;
            EntityUpdateAttribute entityUpdateAttribute = new EntityUpdateAttribute(allowed);
            Assert.True(entityUpdateAttribute.Allowed == allowed);
        }
    }
}
