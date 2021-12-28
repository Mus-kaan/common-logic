//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
namespace Microsoft.Liftr.PerformanceTelemetry.Models.Operations
{
    /// <summary>
    /// List of all the sub operations that are common to multiple RPs and are not RP specific.
    /// </summary>
    public class SubCommonOperations : SubOperationNameBaseType
    {
        public static readonly SubCommonOperations CreateSaaSResourceAsync = new SubCommonOperations("CreateSaaSResourceAsync");
        public static readonly SubCommonOperations DeleteSaaSResourceAsync = new SubCommonOperations("DeleteSaaSResourceAsync");
        public static readonly SubCommonOperations ValidatesSaaSPurchasePaymentAsync = new SubCommonOperations("ValidatesSaaSPurchasePaymentAsync");
        public static readonly SubCommonOperations MigrateSaasResourceAsync = new SubCommonOperations("MigrateSaasResourceAsync");
        public static readonly SubCommonOperations GetSubLevelSaasResourceAsync = new SubCommonOperations("GetSubLevelSaasResourceAsync");
        public static readonly SubCommonOperations GetTenantLevelSaasResourceAsync = new SubCommonOperations("GetTenantLevelSaasResourceAsync");
        public static readonly SubCommonOperations ActivateSaaSSubscriptionAsync = new SubCommonOperations("ActivateSaaSSubscriptionAsync");
        public static readonly SubCommonOperations GetSubscriptionAsync = new SubCommonOperations("GetSubscriptionAsync");

        public SubCommonOperations(string value) : base(value)
        {
        }
    }
}
