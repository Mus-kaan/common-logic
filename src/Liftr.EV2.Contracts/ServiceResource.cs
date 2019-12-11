//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     The object representation of an individual resource in the Cloud Service Model.
    /// </summary>
    public class ServiceResource
    {
        /// <summary>
        ///     The string that can uniquely identify this particular resource. This must match the actual Resource in the Azure subscription.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The name of the <see cref="ServiceResourceDefinition"/> instance that is required to construct this particular resource.
        /// </summary>
        public string InstanceOf { get; set; }

        /// <summary>
        ///     The path to the entity that contains the Azure Resource Model parameters this resource group requires in order to be deployed.
        /// </summary>
        public string ArmParametersPath { get; set; }

        /// <summary>
        ///     The path to the entity that contains the parameters for all rollout actions (if applicable) that'll be run against this resource.
        /// </summary>
        public string RolloutParametersPath { get; set; }

        /// <summary>
        ///     The scope tags for this particular resource.
        /// </summary>
        public IEnumerable<ScopeTag> ScopeTags { get; set; }

        /// <summary>
        /// Gets or sets the string that represents the Azure Resource Group to which the service resource belongs to. For internal use only.
        /// </summary>
        internal string TargetResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets the deployment mode to be used for the ARM deployment.
        /// </summary>
        internal string DeploymentMode { get; set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisResource"/> is reference-equal or value-equal to
        /// <paramref name="otherResource"/>.
        /// </summary>
        /// <param name="thisResource">An instance of <see cref="ServiceResource"/>. </param>
        /// <param name="otherResource">Another instance of <see cref="ServiceResource"/>. </param>
        public static bool operator ==(ServiceResource thisResource, ServiceResource otherResource)
        {
            return Equals(thisResource, otherResource);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisResource"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherResource"/>.
        /// </summary>
        /// <param name="thisResource">An instance of <see cref="ServiceResource"/>. </param>
        /// <param name="otherResource">Another instance of <see cref="ServiceResource"/>. </param>
        public static bool operator !=(ServiceResource thisResource, ServiceResource otherResource)
        {
            return !Equals(thisResource, otherResource);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisResource"/> is reference-equal or value-equal to
        /// <paramref name="otherResource"/>.
        /// </summary>
        /// <param name="thisResource">An instance of <see cref="ServiceResource"/>. </param>
        /// <param name="otherResource">Another instance of <see cref="ServiceResource"/>. </param>
        public static bool Equals(ServiceResource thisResource, ServiceResource otherResource)
        {
            if (ReferenceEquals(thisResource, otherResource))
            {
                return true;
            }

            if (ReferenceEquals(thisResource, null) || ReferenceEquals(otherResource, null))
            {
                return false;
            }

            return thisResource.Name == otherResource.Name &&
                   thisResource.InstanceOf == otherResource.InstanceOf;
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// <paramref name="otherResource"/>.
        /// </summary>
        /// <param name="otherResource">Another instance of <see cref="ServiceResource"/>. </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#",
            Justification = "'otherResource' is more relevant than 'obj'. ")]
        public override bool Equals(object otherResource)
        {
            return Equals(this, otherResource as ServiceResource);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode() +
                   InstanceOf.GetHashCode();
        }
    }
}