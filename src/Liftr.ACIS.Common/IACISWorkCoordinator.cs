//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Common
{
    public interface IACISWorkCoordinator
    {
        Task<ACISWorkResult> StartWorkAsync(string operationName, string parameters, string operationId = null);
    }
}
