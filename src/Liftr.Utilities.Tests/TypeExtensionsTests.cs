//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.Liftr.Utilities.Tests
{
    public class TypeExtensionsTests
    {
        [Fact]
        public void ListConstants()
        {
            var baseConstants = typeof(TestBaseClass).GetConstants();
            Assert.Equal(4, baseConstants.Count());

            var childConstants = typeof(TestDerievedClass).GetConstants();
            Assert.Equal(8, childConstants.Count());

            var baseConstantStrs = typeof(TestBaseClass).GetConstantsValues<string>();
            Assert.Equal(2, baseConstantStrs.Count());

            var childConstantStrs = typeof(TestDerievedClass).GetConstantsValues<string>();
            Assert.Equal(4, childConstantStrs.Count());

            var childConstantInts = typeof(TestDerievedClass).GetConstantsValues<int>();
            Assert.Equal(4, childConstantInts.Count());
        }
    }

#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
    public class TestBaseClass
#pragma warning restore CA1052 // Static holder types should be Static or NotInheritable
    {
        public const string BaseConstStr1 = "apple";
        public const string BaseConstStr2 = "plum";
        public const int BaseConstInt1 = -1;
        public const int BaseConstInt2 = 2;
    }

#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
    public class TestDerievedClass : TestBaseClass
#pragma warning restore CA1052 // Static holder types should be Static or NotInheritable
    {
        public const string ChildConstStr1 = "chile-apple";
        public const string ChildConstStr2 = "child-plum";
        public const int ChildConstInt1 = -101;
        public const int ChildConstInt2 = 201;
    }
}
