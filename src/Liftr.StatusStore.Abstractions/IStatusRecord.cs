//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.StatusStore
{
    public interface IStatusRecord : IWriterMetaData
    {
        public string Key { get; set; }

        public DateTime TimeStamp { get; set; }

        public string CorrelationId { get; set; }

        public string Value { get; set; }
    }
}
