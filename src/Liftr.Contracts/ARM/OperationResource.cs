//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts.ARM
{
    /// <summary>
    /// An operation resource.
    /// </summary>
    public class OperationResource
    {
        /// <summary>
        /// The operation status id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The status of the operation.
        /// </summary>
        public ProvisioningState Status { get; set; }

        /// <summary>
        /// The status of the operation.
        /// </summary>
        public OperationError Error { get; set; }
    }

    /// <summary>
    /// The operation error.
    /// </summary>
    public class OperationError
    {
        /// <summary>
        /// The operation error code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The operation error message.
        /// </summary>
        public string Message { get; set; }
    }
}
