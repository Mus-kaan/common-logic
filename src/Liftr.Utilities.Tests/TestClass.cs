//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Utilities.Tests
{
    public enum TestEnum
    {
        TestEnumValue1,
        SkuPremium,
        ALLCAP,
        Snake_Value,
        Basic,
        PremiumSKU,
    }

    public class TestClass
    {
        public string StrProp { get; set; } = "string value a.";

        public int IntProp { get; set; } = -1222;

        public DateTime TimeStampInZulu { get; set; } = "2019-07-05T15:50:54.1793804Z".ParseZuluDateTime();

        public TimeSpan TimeSpanValue { get; set; } = TimeSpan.FromMinutes(55.34);

        public TestEnum EnumValue1 { get; set; } = TestEnum.TestEnumValue1;

        public TestEnum EnumValue2 { get; set; } = TestEnum.SkuPremium;

        public TestEnum EnumValue3 { get; set; } = TestEnum.ALLCAP;

        public TestEnum EnumValue4 { get; set; } = TestEnum.Snake_Value;

        public TestEnum EnumValue5 { get; set; } = TestEnum.Basic;

        public TestEnum EnumValue6 { get; set; } = TestEnum.PremiumSKU;
    }
}
