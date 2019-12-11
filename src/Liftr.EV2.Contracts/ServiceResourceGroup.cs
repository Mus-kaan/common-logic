//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     The object representation a resource group in the Cloud Service Model.
    /// </summary>
    public class ServiceResourceGroup
    {
        /// <summary>
        /// The name of the service resource group.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "Liftr1102:Non-public fields should start with _ or s_", Justification = "This is copied code.")]
        private string name;

        /// <summary>
        ///     The string that can uniquely identify this particular resource group. This must match the actual Resource Group name in the Azure subscription.
        /// </summary>
        public string AzureResourceGroupName { get; set; }

        /// <summary>
        ///     Location of the resource group instance.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        ///     The name of the template that defines how to construct this particular resource group.
        /// </summary>
        public string InstanceOf { get; set; }

        /// <summary>
        ///     The various resources that constitute this particular resource group.
        /// </summary>
        public IEnumerable<ServiceResource> ServiceResources { get; set; }

        /// <summary>
        ///     The Azure Subscription ID that this particular resource group is associated with.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Guid AzureSubscriptionId { get; set; }

        /// <summary>
        ///     The Scope Tags of this particular resource group.
        /// </summary>
        public IEnumerable<ScopeTag> ScopeTags { get; set; }

        /// <summary>
        /// Gets or sets the name of the service resource group.
        /// </summary>
        internal string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = AzureResourceGroupName;
                }

                return name;
            }

            set
            {
                name = value;
            }
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisResourceGroup"/> is reference-equal or value-equal to
        /// <paramref name="otherResourceGroup"/>.
        /// </summary>
        /// <param name="thisResourceGroup">An instance of <see cref="ServiceResourceGroup"/>. </param>
        /// <param name="otherResourceGroup">Another instance of <see cref="ServiceResourceGroup"/>. </param>
        public static bool operator ==(ServiceResourceGroup thisResourceGroup, ServiceResourceGroup otherResourceGroup)
        {
            return Equals(thisResourceGroup, otherResourceGroup);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisResourceGroup"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherResourceGroup"/>.
        /// </summary>
        /// <param name="thisResourceGroup">An instance of <see cref="ServiceResourceGroup"/>. </param>
        /// <param name="otherResourceGroup">Another instance of <see cref="ServiceResourceGroup"/>. </param>
        public static bool operator !=(ServiceResourceGroup thisResourceGroup, ServiceResourceGroup otherResourceGroup)
        {
            return !Equals(thisResourceGroup, otherResourceGroup);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisResourceGroup"/> is reference-equal or value-equal to
        /// <paramref name="otherResourceGroup"/>.
        /// </summary>
        /// <param name="thisResourceGroup">An instance of <see cref="ServiceResourceGroup"/>. </param>
        /// <param name="otherResourceGroup">Another instance of <see cref="ServiceResourceGroup"/>. </param>
        public static bool Equals(ServiceResourceGroup thisResourceGroup, ServiceResourceGroup otherResourceGroup)
        {
            if (ReferenceEquals(thisResourceGroup, otherResourceGroup))
            {
                return true;
            }

            if (ReferenceEquals(thisResourceGroup, null) || ReferenceEquals(otherResourceGroup, null))
            {
                return false;
            }

            return thisResourceGroup.AzureSubscriptionId == otherResourceGroup.AzureSubscriptionId &&
                   thisResourceGroup.AzureResourceGroupName.Equals(
                   otherResourceGroup.AzureResourceGroupName, StringComparison.OrdinalIgnoreCase) &&
                   thisResourceGroup.Location.Equals(
                   otherResourceGroup.Location, StringComparison.OrdinalIgnoreCase) &&
                   thisResourceGroup.InstanceOf.Equals(
                   otherResourceGroup.InstanceOf, StringComparison.OrdinalIgnoreCase) &&
                   thisResourceGroup.ServiceResources.IsEquivalentTo(otherResourceGroup.ServiceResources);
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// <paramref name="otherResourceGroup"/>.
        /// </summary>
        /// <param name="otherResourceGroup">Another instance of <see cref="ServiceResourceGroup"/>. </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#",
            Justification = "'otherResourceGroup' is more relevant than 'obj'. ")]
        public override bool Equals(object otherResourceGroup)
        {
            return Equals(this, otherResourceGroup as ServiceResourceGroup);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            return AzureSubscriptionId.GetHashCode() +
                   AzureResourceGroupName.GetHashCode() +
                   InstanceOf.GetHashCode();
        }
    }
}