//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CloudType
    {
        Public,
        DogFood,
        Fairfax,
        Mooncake,

        /// <summary>
        /// USNat(EX)
        /// </summary>
        USNat,

        /// <summary>
        /// USSec(RX)
        /// </summary>
        USSec,
    }
}
