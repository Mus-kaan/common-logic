//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Net.Http;

namespace Microsoft.Liftr.Marketplace.Exceptions
{
    public class PurchaseFailureException : PollingException
    {
        public PurchaseFailureException(string message)
            : base(message)
        {
        }

        public PurchaseFailureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public PurchaseFailureException()
        {
        }

        public PurchaseFailureException(
            string message,
            string rawErrorMessage,
            PurchaseErrorType purchaseErrorType,
            Uri operationUri,
            HttpRequestMessage originalRequest,
            HttpResponseMessage responseMessage)
            : base(message, originalRequest, operationUri, responseMessage)
        {
            ErrorType = purchaseErrorType;
            RawErrorMessage = rawErrorMessage;
        }

        /// <summary>
        /// Type of the error related to the Purchase Failure
        /// </summary>
        public PurchaseErrorType ErrorType { get; set; }

        /// <summary>
        /// Raw Error Message returned by Marketplace in case of Purchase Failure
        /// </summary>
        public string RawErrorMessage { get; set; }
    }
}
