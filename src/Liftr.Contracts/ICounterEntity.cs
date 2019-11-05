//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts
{
    public interface ICounterEntity
    {
        /// <summary>
        /// Key of the counter. This will be unique and indexed.
        /// </summary>
        string CounterKey { get; }

        int CounterValue { get; set; }

        DateTime CreatedUTC { get; }

        DateTime LastModifiedUTC { get; set; }
    }
}
