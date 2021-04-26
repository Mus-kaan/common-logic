//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Utils;
using System.Net;

namespace Microsoft.Liftr.Marketplace.ARM.Models
{
    public class PaymentValidationResponse
    {
        public bool IsSuccess { get; set; }

        public PaymentValidationError Error { get; set; }

        public PurchaseErrorType? ErrorType { get; set; }

        public static PaymentValidationResponse BuildValidationResponseSuccessful()
        {
            return new PaymentValidationResponse()
            {
                IsSuccess = true,
                Error = null,
                ErrorType = null,
            };
        }

        public static PaymentValidationResponse BuildValidationResponseFailed(HttpStatusCode code, string message)
        {
            PurchaseErrorType? errorType = null;

            if (!PurchaseExceptionHelper.TryGetPurchaseErrorType(message, out errorType))
            {
                errorType = PurchaseErrorType.PaymentValidationFailedWithUnknownIssue;
            }

            return new PaymentValidationResponse()
            {
                IsSuccess = false,
                Error = new PaymentValidationError()
                {
                    Code = code,
                    Message = message,
                },
                ErrorType = errorType,
            };
        }
    }

    public class PaymentValidationError
    {
        public HttpStatusCode Code { get; set; }

        public string Message { get; set; }
    }
}
