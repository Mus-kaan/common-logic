//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Worker
{
    public interface IACISOperationProcessor
    {
        Task ProcessAsync(ACISOperationRequest request);
    }
}
