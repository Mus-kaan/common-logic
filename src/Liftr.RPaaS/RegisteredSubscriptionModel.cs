//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Microsoft.Liftr.RPaaS
{
    /// <summary>
    /// More information at https://github.com/Azure/azure-resource-manager-rpc/blob/master/v1.0/subscription-lifecycle-api-reference.md#subscription-lifecycle-api-reference.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SubscriptionState
    {
        Registered,
        Unregistered,
        Warned,
        Suspended,
        Deleted,
    }

    /// <summary>
    /// Details of the account owner.
    /// More information at https://github.com/Azure/azure-resource-manager-rpc/blob/master/v1.0/subscription-lifecycle-api-reference.md#subscription-lifecycle-api-reference.
    /// </summary>
    public class SubscriptionAccountOwner
    {
        /// <summary>
        /// Personalized User Identification.
        /// </summary>
        public string Puid { get; set; }

        /// <summary>
        /// Email of the account owner.
        /// </summary>
        public string Email { get; set; }
    }

    /// <summary>
    /// Class defining a registered feature.
    /// </summary>
    public class RegisteredFeature
    {
        /// <summary>
        /// Feature id.
        /// </summary>
        public string FeatureId { get; set; }
    }

    /// <summary>
    /// Class defining a tenant that manages the registration.
    /// </summary>
    public class ManagerTenant
    {
        /// <summary>
        /// Tenant id.
        /// </summary>
        public string TenantId { get; set; }
    }

    /// <summary>
    /// More information at https://github.com/Azure/azure-resource-manager-rpc/blob/master/v1.0/subscription-lifecycle-api-reference.md#subscription-lifecycle-api-reference.
    /// </summary>
    public class RegisteredSubscriptionModel
    {
        /// <summary>
        /// the ID of the subscription.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Optional. The AAD directory/tenant to which the subscription belongs.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// The index state of the registration.
        /// </summary>
        public string IndexState { get; set; }

        /// <summary>
        /// Required. One of "Registered", "Unregistered", "Warned", "Suspended" , or
        /// "Deleted"; used to indicate the current state of the subscription. The
        /// resource provider should always take the latest state. Transition among
        /// any states is valid (for example - it is possible to receive
        /// Suspended / Warned before Registered). Details on these states could be
        /// found at https://github.com/Azure/azure-resource-manager-rpc/blob/master/v1.0/subscription-lifecycle-api-reference.md#subscription-states.
        /// </summary>
        public SubscriptionState SubscriptionState { get; set; }

        /// <summary>
        /// Optional. The placement requirement for the subscription based on its country
        /// of origin / offer type / offer category / etc. This is used in geo-fencing
        /// of certain regions or regulatory boundaries (e.g. Australia ring-fencing).
        /// </summary>
        public string LocationPlacementId { get; set; }

        /// <summary>
        /// Optional. The quota requirement for the subscription based on the offer
        /// type / category (e.g. free vs. pay-as-you-go). This can be used to inform
        /// quota information for the subscription (e.g. max # of resource groups or
        /// max # of virtual machines).
        /// </summary>
        public string QuotaId { get; set; }

        /// <summary>
        /// Boolean for subscription spending limit.
        /// </summary>
        public bool SubscriptionSpendingLimit { get; set; }

        /// <summary>
        /// String for Account owner.
        /// </summary>
        public SubscriptionAccountOwner AccountOwner { get; set; }

        /// <summary>
        /// Optional. All AFEC features that the subscriptions has been registered under
        /// RP namespace and platform namespace (Microsoft.Resources). Null or an empty
        /// array would mean that there are no registered features in the subscription.
        /// </summary>
        public IEnumerable<RegisteredFeature> RegisteredFeatures { get; set; }

        /// <summary>
        /// Optional. All tenants managing the subscription. Null or empty means that
        /// there are no managing tenants.
        /// </summary>
        public IEnumerable<ManagerTenant> ManagedByTenants { get; set; }

        /// <summary>
        /// Additional properties of the registration.
        /// </summary>
        public IDictionary<string, string> AdditionalProperties { get; set; }
    }
}
