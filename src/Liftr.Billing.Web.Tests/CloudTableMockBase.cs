//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Liftr.Billing.Web.Tests
{
    public abstract class CloudTableMockBase : Mock<CloudTable>
    {
        protected CloudTableMockBase(MockBehavior behavior, Uri address, TableClientConfiguration configuration = null)
            : base(behavior, address, configuration)
        {
        }

        protected static TableQuerySegment<T> GetQuerySegment<T>(List<T> entities)
        {
            // this type only has internal constructors unfortunately
            return (TableQuerySegment<T>)typeof(TableQuerySegment<T>)
                .GetConstructor(
                    bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
                    binder: null,
                    types: new Type[] { typeof(List<T>) },
                    modifiers: null)
                .Invoke(new object[] { entities });
        }
    }
}