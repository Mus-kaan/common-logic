//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Liftr.AzureAsyncOperation.DataSource;
using System.Threading.Tasks;

namespace Microsoft.Liftr.AzureAsyncOperation
{
    public abstract class AsyncOperationsBaseController : ControllerBase
    {
        private readonly IAsyncOperationStatusDataSource _asyncOperationStatusDataSource;

        protected AsyncOperationsBaseController(IAsyncOperationStatusDataSource asyncOperationStatusDataSource)
        {
            _asyncOperationStatusDataSource = asyncOperationStatusDataSource;
        }

        /// <summary>
        /// Retrieves an async operation resource to track an async operation
        /// Used to implement the behavior of Azure async operations provided by the Azure-AsyncOperation header
        /// </summary>
        [HttpGet]
        [Route(Constants.AsyncOperationRoute)]
        public async Task<IActionResult> GetOperationStatusAsync(
            string subscriptionId,
            string provider,
            string location,
            string operationId)
        {
            var operation = await _asyncOperationStatusDataSource.GetAsync(operationId);

            // For invalid operations id, return 404- Not Found
            if (operation == null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return NotFound();
            }

            return Ok(operation.Resource);
        }
    }
}
