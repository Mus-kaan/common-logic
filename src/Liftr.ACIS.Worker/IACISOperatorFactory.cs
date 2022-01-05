//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ACIS.Worker
{
    public interface IACISOperatorFactory
    {
       IACISOperationProcessor GetOperator(string OperationName);
    }
}
