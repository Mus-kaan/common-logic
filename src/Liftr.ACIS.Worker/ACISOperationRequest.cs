//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.ACIS.Relay;

namespace Microsoft.Liftr.ACIS.Worker
{
    public class ACISOperationRequest
    {
        public IACISOperation Operation { get; set; }

        public string OperationName { get; set; }

        public string OperationId { get; set; }

        public string Parameters { get; set; }
    }
}
