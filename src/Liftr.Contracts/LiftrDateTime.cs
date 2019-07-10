//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Liftr.Contracts
{
    public static class LiftrDateTime
    {
        public static DateTime MinValue => DateTime.Parse("0001-01-01T00:00:00Z", CultureInfo.InvariantCulture).ToUniversalTime();
    }
}
