//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     The object representation of the definition of a particular resource group in the Cloud Service Model.
    /// </summary>
    public class ServiceResourceGroupDefinition
    {
        /// <summary>
        ///     The human-readable name of the definition.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The enumeration of the various resource definitions that represent how to construct
        ///     the resources that constitute this resource group definition.
        /// </summary>
        public IEnumerable<ServiceResourceDefinition> ServiceResourceDefinitions { get; set; }

        /// <summary>
        /// Gets or sets the Policy information to be used for evaluating this resource group.
        /// </summary>
        public Policy Policy { get; set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisDefinition"/> is reference-equal or value-equal to
        /// <paramref name="otherDefinition"/>.
        /// </summary>
        /// <param name="thisDefinition">An instance of <see cref="ServiceResourceGroupDefinition"/>. </param>
        /// <param name="otherDefinition">Another instance of <see cref="ServiceResourceGroupDefinition"/>. </param>
        public static bool operator ==(ServiceResourceGroupDefinition thisDefinition, ServiceResourceGroupDefinition otherDefinition)
        {
            return Equals(thisDefinition, otherDefinition);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisDefinition"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherDefinition"/>.
        /// </summary>
        /// <param name="thisDefinition">An instance of <see cref="ServiceResourceGroupDefinition"/>. </param>
        /// <param name="otherDefinition">Another instance of <see cref="ServiceResourceGroupDefinition"/>. </param>
        public static bool operator !=(ServiceResourceGroupDefinition thisDefinition, ServiceResourceGroupDefinition otherDefinition)
        {
            return !Equals(thisDefinition, otherDefinition);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisDefinition"/> is reference-equal or value-equal to
        /// <paramref name="otherDefinition"/>.
        /// </summary>
        /// <param name="thisDefinition">An instance of <see cref="ServiceResourceGroupDefinition"/>. </param>
        /// <param name="otherDefinition">Another instance of <see cref="ServiceResourceGroupDefinition"/>. </param>
        public static bool Equals(ServiceResourceGroupDefinition thisDefinition, ServiceResourceGroupDefinition otherDefinition)
        {
            if (ReferenceEquals(thisDefinition, otherDefinition))
            {
                return true;
            }

            if (ReferenceEquals(thisDefinition, null) || ReferenceEquals(otherDefinition, null))
            {
                return false;
            }

            return thisDefinition.Name == otherDefinition.Name &&
                   thisDefinition.ServiceResourceDefinitions.IsEquivalentTo(otherDefinition.ServiceResourceDefinitions);
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// <paramref name="otherDefinition"/>.
        /// </summary>
        /// <param name="otherDefinition">Another instance of <see cref="ServiceResourceGroupDefinition"/>. </param>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#",
            Justification = "'otherDefinition' is more relevant than 'obj'. ")]
        public override bool Equals(object otherDefinition)
        {
            return Equals(this, otherDefinition as ServiceResourceGroupDefinition);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}