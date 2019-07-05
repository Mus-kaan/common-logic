//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Liftr.Contracts
{
    public class MockTimeSource : ITimeSource
    {
        public DateTime Current { get; set; } = DateTimeOffset.Parse("2019-01-20T08:00:00+00:00", CultureInfo.InvariantCulture).ToUniversalTime().UtcDateTime;

        public DateTime UtcNow => Current;

        public void Add(TimeSpan value)
        {
            Current = Current.Add(value);
        }
    }
}
