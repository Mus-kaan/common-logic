//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Exceptions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Microsoft.Liftr.Marketplace.Utils
{
    public static class PurchaseExceptionHelper
    {
        public static readonly Dictionary<string, PurchaseErrorType> CommerceErrorMapping = new Dictionary<string, PurchaseErrorType>()
        {
            { CommerceErrorMessages.OperationErrorNoValidPIForPurchase, PurchaseErrorType.MissingPaymentInstrument },
            { CommerceErrorMessages.OperationErrorProductNotForCsp, PurchaseErrorType.NotAllowedForCSPSubscriptions },
            { CommerceErrorMessages.OperationErrorPurchaseMarketplaceDisabled, PurchaseErrorType.MarketplaceNotAllowedForSubscriptions },
            { CommerceErrorMessages.OperationErrorAzureSubscriptionMissingForPurchase, PurchaseErrorType.SubscriptionNotFoundForBilling },
            { CommerceErrorMessages.OperationErrorPaidPurchaseWithFreeTrial, PurchaseErrorType.FreeSubscriptionNotAllowed },
            { CommerceErrorMessages.OperationErrorProductNotForCspByCsp, PurchaseErrorType.CSPTenantNotAllowedForPurchase },
            { CommerceErrorMessages.TestHeaderRetentionExpired, PurchaseErrorType.TestHeaderExpired },
            { CommerceErrorMessages.PlanNotAvailableForPurchasing, PurchaseErrorType.PlanNotAvailableForPurchase },
            { CommerceErrorMessages.UnknownPaymentValidationIssue, PurchaseErrorType.PaymentValidationFailedWithUnknownIssue },
        };

        internal static bool TryGetPurchaseFailure(
            string errorMessage,
            Uri operationUri,
            HttpRequestMessage originalRequest,
            HttpResponseMessage responseMessage,
            out PurchaseFailureException exception)
        {
            foreach (KeyValuePair<string, PurchaseErrorType> entry in CommerceErrorMapping)
            {
                if (Regex.IsMatch(errorMessage, entry.Key))
                {
                    var errorType = entry.Value;
                    var message = ExceptionMessageUtils.BuildPurchaseFailureMessage(originalRequest, operationUri, errorType, errorMessage);
                    exception = new PurchaseFailureException(message.ToString(), errorMessage, errorType, operationUri, originalRequest, responseMessage);
                    return true;
                }
            }

            exception = null;
            return false;
        }

        internal static bool TryGetPurchaseErrorType(
           string errorMessage, out PurchaseErrorType? errorType)
        {
            foreach (KeyValuePair<string, PurchaseErrorType> entry in CommerceErrorMapping)
            {
                if (Regex.IsMatch(errorMessage, entry.Key))
                {
                    errorType = entry.Value;
                    return true;
                }
            }

            errorType = null;
            return false;
        }
    }
}
