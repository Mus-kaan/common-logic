//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json.Converters;
using System.Globalization;

namespace Microsoft.Liftr.Utilities
{
    public class ZuluDateTimeConverter : IsoDateTimeConverter
    {
        public ZuluDateTimeConverter()
        {
            DateTimeStyles = DateTimeStyles.AdjustToUniversal;
        }
    }
}
