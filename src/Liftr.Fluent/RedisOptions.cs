//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Liftr.Fluent
{
    public static class RedisOptions
    {
        public const int defaultCapacity = 1;

        public const int defaultShardCount = 0;

        public const RedisSkuOption defaultSku = RedisSkuOption.Standard;

        public enum RedisSkuOption
        {
            Basic,
            Standard,
            Premium,
            Enterprise,
            EnterpriseFlash,
        }
    }
}
