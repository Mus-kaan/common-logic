//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     The object representation of the definition of a particular resource in the Cloud Service Model.
    /// </summary>
    public class ServiceResourceDefinition
    {
        /// <summary>
        ///     The human-readable name of the definition.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The path to the entity that contains the Azure Resource Model template for this particular definition.
        /// </summary>
        public string ArmTemplatePath { get; set; }

        /// <summary>
        /// Deployment Composition parts applicable for this service resource definition
        /// </summary>
        public CompositionParts ComposedOf { get; set; }

        /// <summary>
        ///     The string representation of the various entities that consume this particular resource.
        /// </summary>
        public IEnumerable<string> ProducerMonikers { get; set; }

        /// <summary>
        ///     The string representation of the various entities that this resource depends on.
        /// </summary>
        public IEnumerable<string> ConsumerMonikers { get; set; }

        /// <summary>
        /// Gets or sets the Policy information to be used for evaluating this resource.
        /// </summary>
        public Policy Policy { get; set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisDefinition"/> is reference-equal or value-equal to
        /// <paramref name="otherDefinition"/>.
        /// </summary>
        /// <param name="thisDefinition">An instance of <see cref="ServiceResourceDefinition"/>. </param>
        /// <param name="otherDefinition">Another instance of <see cref="ServiceResourceDefinition"/>. </param>
        public static bool operator ==(ServiceResourceDefinition thisDefinition, ServiceResourceDefinition otherDefinition)
        {
            return Equals(thisDefinition, otherDefinition);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisDefinition"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherDefinition"/>.
        /// </summary>
        /// <param name="thisDefinition">An instance of <see cref="ServiceResourceDefinition"/>. </param>
        /// <param name="otherDefinition">Another instance of <see cref="ServiceResourceDefinition"/>. </param>
        public static bool operator !=(ServiceResourceDefinition thisDefinition, ServiceResourceDefinition otherDefinition)
        {
            return !Equals(thisDefinition, otherDefinition);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisDefinition"/> is reference-equal or value-equal to
        /// <paramref name="otherDefinition"/>.
        /// </summary>
        /// <param name="thisDefinition">An instance of <see cref="ServiceResourceDefinition"/>. </param>
        /// <param name="otherDefinition">Another instance of <see cref="ServiceResourceDefinition"/>. </param>
        public static bool Equals(ServiceResourceDefinition thisDefinition, ServiceResourceDefinition otherDefinition)
        {
            if (ReferenceEquals(thisDefinition, otherDefinition))
            {
                return true;
            }

            if (ReferenceEquals(thisDefinition, null) || ReferenceEquals(otherDefinition, null))
            {
                return false;
            }

            return thisDefinition.Name == otherDefinition.Name;
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// <paramref name="otherDefinition"/>.
        /// </summary>
        /// <param name="otherDefinition">Another instance of <see cref="ServiceResourceDefinition"/>. </param>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#",
            Justification = "'otherDefinition' is more relevant than 'obj'. ")]
        public override bool Equals(object otherDefinition)
        {
            return Equals(this, otherDefinition as ServiceResourceDefinition);
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