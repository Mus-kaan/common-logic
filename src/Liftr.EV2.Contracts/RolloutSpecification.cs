//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     The object representation of the specification that dictates the particular sequence of actions that will
    ///     take place as part of a new Deployment.
    /// </summary>
    public class RolloutSpecification : Document
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RolloutSpecification"/> class.
        /// </summary>
        public RolloutSpecification()
        {
            OrchestratedSteps = Enumerable.Empty<RolloutStep>();
            Schema = new System.Uri("http://schema.express.azure.com/schemas/2015-01-01-alpha/RolloutSpec.json");
        }

        /// <summary>
        ///     The metadata associated with this particular rollout.
        /// </summary>
        public RolloutMetadata RolloutMetadata { get; set; }

        /// <summary>
        ///     The exact sequence of steps that must be executed as part of this rollout.
        /// </summary>
        public IEnumerable<RolloutStep> OrchestratedSteps { get; set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisSpecification"/> is reference-equal or value-equal to
        /// <paramref name="otherSpecification"/>.
        /// </summary>
        /// <param name="thisSpecification">An instance of <see cref="RolloutSpecification"/>. </param>
        /// <param name="otherSpecification">Another instance of <see cref="RolloutSpecification"/>. </param>
        public static bool operator ==(RolloutSpecification thisSpecification, RolloutSpecification otherSpecification)
        {
            return Equals(thisSpecification, otherSpecification);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisSpecification"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherSpecification"/>.
        /// </summary>
        /// <param name="thisSpecification">An instance of <see cref="RolloutSpecification"/>. </param>
        /// <param name="otherSpecification">Another instance of <see cref="RolloutSpecification"/>. </param>
        public static bool operator !=(RolloutSpecification thisSpecification, RolloutSpecification otherSpecification)
        {
            return !Equals(thisSpecification, otherSpecification);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisSpecification"/> is reference-equal or value-equal to
        /// <paramref name="otherSpecification"/>.
        /// </summary>
        /// <param name="thisSpecification">An instance of <see cref="RolloutSpecification"/>. </param>
        /// <param name="otherSpecification">Another instance of <see cref="RolloutSpecification"/>. </param>
        public static bool Equals(RolloutSpecification thisSpecification, RolloutSpecification otherSpecification)
        {
            if (Document.ReferenceEquals(thisSpecification, otherSpecification))
            {
                return true;
            }

            if (Document.ReferenceEquals(thisSpecification, null) || Document.ReferenceEquals(otherSpecification, null))
            {
                return false;
            }

            return thisSpecification.RolloutMetadata == otherSpecification.RolloutMetadata &&
                   thisSpecification.OrchestratedSteps.IsEquivalentTo(otherSpecification.OrchestratedSteps);
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// <paramref name="otherSpecification"/>.
        /// </summary>
        /// <param name="otherSpecification">Another instance of <see cref="RolloutSpecification"/>. </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#",
            Justification = "'otherSpecification' is more relevant than 'obj'. ")]
        public override bool Equals(object otherSpecification)
        {
            return Equals(this, otherSpecification as RolloutSpecification);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            return RolloutMetadata.GetHashCode();
        }
    }
}