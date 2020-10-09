//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Relay
{
    public interface IACISOperationStatusEntityDataSource
    {
        Task<ACISOperationStatusEntity> GetEntityAsync(string operationName, string operationId);

        Task<ACISOperationStatusEntity> UpdateEntityAsync(ACISOperationStatusEntity entity);
    }
}
