//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Marketplace.Exceptions
{
#nullable enable
    /// <summary>
    /// This class contains ErrorCodes for Marketplace purchase errors. Corresponding error messages can be seen <see href="https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Providers%2FConstants%2FCommerceOperationErrorMessages.cs">Marketplace side</see>
    /// </summary>
    public enum PurchaseErrorType
    {
        MissingPaymentInstrument,
        NotAllowedForCSPSubscriptions,
        MarketplaceNotAllowedForSubscriptions,
        SubscriptionNotFoundForBilling,
        FreeSubscriptionNotAllowed,
        CSPTenantNotAllowedForPurchase,
    }
}
