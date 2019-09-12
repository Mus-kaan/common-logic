//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.AzureAsyncOperation.DataSource
{
    public interface IAsyncOperationStatusDataSource
    {
        /// <summary>
        /// Updates status for azure async operaion
        /// </summary>
        /// <param name="status"></param>
        /// <param name="timeout"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        Task<AsyncOperationStatusEntity> CreateAsync(
            OperationStatus status,
            TimeSpan timeout,
            string errorCode = null,
            string errorMessage = null);

        /// <summary>
        /// Updates status for azure async operaion
        /// </summary>
        /// <param name="operationId"></param>
        /// <param name="status"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        Task<AsyncOperationStatusEntity> UpdateAsync(
            string operationId,
            OperationStatus status,
            string errorCode = null,
            string errorMessage = null);

        /// <summary>
        /// Retrieves status for azure async operation
        /// </summary>
        /// <param name="operationId"></param>
        /// <returns></returns>
        Task<AsyncOperationStatusEntity> GetAsync(string operationId);
    }
}
