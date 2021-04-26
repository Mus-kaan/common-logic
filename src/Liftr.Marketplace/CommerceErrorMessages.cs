//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Marketplace
{
    /// <summary>
    /// Error Messages received from Marketplace when there is a Commerce failure. Here is the file on <see href="https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Providers%2FConstants%2FCommerceOperationErrorMessages.cs">Marketplace side</see>
    /// </summary>
    /// <remarks>
    /// This class contains the pattern of the error messages returned by Marketplace."".+"" stands for placeholders and is used for regex parsing of Offer specific words that appear in error messages.
    /// </remarks>
    public static class CommerceErrorMessages
    {
        /// <summary>
        /// Error message for when the product is not eligible for CSP
        /// </summary>
        public static readonly string OperationErrorProductNotForCsp = "This offer is not available for purchasing by subscriptions belonging to Microsoft Azure Cloud Solution Providers.";

        /// <summary>
        /// Error message for when the product is not eligible for CSP
        /// </summary>
        public static readonly string OperationErrorProductNotForCspByCsp = "Your CSP tenant is not eligible to purchase this offer.Please contact the publisher";

        /// <summary>
        /// Error message for when the product is not eligible for CSP
        /// </summary>
        public static readonly string OperationErrorProductNotForCspByCspWithoutDetails = "Your CSP tenant is not eligible to purchase this offer.Please contact the publisher";

        /// <summary>
        /// Error message for when Azure Marketplace is disabled in the Azure subscription attempting the purchase
        /// </summary>
        public static readonly string OperationErrorPurchaseMarketplaceDisabled = "Subscription used for this purchase doesn't allow Marketplace purchases. Use different subscription or ask your administrator to change definition for this subscription and retry.";

        /// <summary>
        /// Error message for when the Azure Subscription is not found during purchase
        /// </summary>
        public static readonly string OperationErrorAzureSubscriptionMissingForPurchase = @"Purchase has failed because we could not find Azure subscription with id \S+ provided for billing.";

        /// <summary>
        /// Error message for when no PI could be found in the Azure Subscription for purchase
        /// </summary>
        public static readonly string OperationErrorNoValidPIForPurchase = "Purchase has failed because we couldn't find a valid credit card";

        /// <summary>
        /// Error message for when free trial Azure Subscription tries to purchase a paid offer
        /// </summary>
        public static readonly string OperationErrorPaidPurchaseWithFreeTrial = @"The plan "".+"" can not be purchased on a free subscription, please upgrade your account, see https://aka.ms/UpgradeFreeSub for more details";

        /// <summary>
        /// Error message for signing the agreement during purchase
        /// </summary>
        public static readonly string OperationErrorPurchaseAgreementSigning = "Purchase has failed because we couldn't verify your signing on legal agreement. Please retry. If error persists, try to make the purchase using different Azure subscription or contact support.";

        /// <summary>
        /// Error message when a subscription is not eligible to purchase a sku in the private store context
        /// </summary>
        public static readonly string OperationErrorNotEligibleSkuForPrivateStore = @"Plan "".+"" of offer "".+"" by publisher "".+"" is not available to you for purchase per the rules set by your IT Admin";

        /// <summary>
        /// Error message when a subscription is not eligible to purchase because of test header expiration
        /// </summary>
        public static readonly string TestHeaderRetentionExpired = @"Invalid Subscription id \S+, Test header retention date cannot be in the past";

        /// <summary>
        /// Error message when a plan is not eligible for purchasing
        /// </summary>
        public static readonly string PlanNotAvailableForPurchasing = @"Purchase of plan "".+"" of offer "".+"" by publisher "".+"" has failed. This plan is currently not available for purchasing.";

        /// <summary>
        /// Error message when an unknown payment validation issue occurred
        /// </summary>
        public static readonly string UnknownPaymentValidationIssue = @"Some payment validation issue happened for the subscription id \S+, Please retry. If error persists, try to make the purchase using different Azure subscription or contact support.";
    }
}
