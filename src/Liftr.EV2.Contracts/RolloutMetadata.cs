//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     An enumeration of various deployment types.
    /// </summary>
    public enum RolloutType
    {
        /// <summary>
        ///     If rollout type is not defined, set as none(default).
        /// </summary>
        None = 0,

        /// <summary>
        ///     Represents a significant service update that may contain breaking changes.
        /// </summary>
        Major = 1,

        /// <summary>
        ///     Represents a minor service update that typically contains bug fixes and
        ///     does not introduce any breaking changes.
        /// </summary>
        Minor = 2,

        /// <summary>
        ///     Represents a critical update that is highly targeted and intended to resolve
        ///     a breaking-issue in a production environment.
        /// </summary>
        Hotfix = 3,
    }

    /// <summary>
    ///     A class that contains information that can uniquely identify a rollout.
    /// </summary>
    public class RolloutMetadata
    {
        /// <summary>
        /// Current metadata version
        /// </summary>
        public const string Version20160101 = "2016-06-01";

        /// <summary>
        /// Initializes a new instance of the <see cref="RolloutMetadata"/> class.
        /// </summary>
        public RolloutMetadata()
        {
            Version = RolloutMetadata.Version20160101;
        }

        /// <summary>
        /// Gets or sets Metadata version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///     The path relative to the Service Group Root that points to the service model of the service that is being updated as part of this rollout.
        /// </summary>
        public string ServiceModelPath { get; set; }

        /// <summary>
        /// The path relative to the Service Group Root that points to the scope bindings.
        /// </summary>
        public string ScopeBindingsPath { get; set; }

        /// <summary>
        /// The user-specified name of this particular rollout.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The scope of this particular rollout.
        /// </summary>
        public RolloutType RolloutType { get; set; }

        /// <summary>
        ///     The location of the build to use for this particular rollout.
        /// </summary>
        public BuildSource BuildSource { get; set; }

        /// <summary>
        /// Gets or sets Notification settings
        /// </summary>
        public RolloutNotification Notification { get; set; }

        /// <summary>
        /// Gets or sets the rollout policies to be used for the rollout.
        /// </summary>
        public IEnumerable<RolloutPolicyReference> RolloutPolicyReferences { get; set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisMetadata"/> is reference-equal or value-equal to
        /// <paramref name="otherMetadata"/>.
        /// </summary>
        /// <param name="thisMetadata">An instance of <see cref="RolloutMetadata"/>. </param>
        /// <param name="otherMetadata">Another instance of <see cref="RolloutMetadata"/>. </param>
        public static bool operator ==(RolloutMetadata thisMetadata, RolloutMetadata otherMetadata)
        {
            return Equals(thisMetadata, otherMetadata);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisMetadata"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherMetadata"/>.
        /// </summary>
        /// <param name="thisMetadata">An instance of <see cref="RolloutMetadata"/>. </param>
        /// <param name="otherMetadata">Another instance of <see cref="RolloutMetadata"/>. </param>
        public static bool operator !=(RolloutMetadata thisMetadata, RolloutMetadata otherMetadata)
        {
            return !Equals(thisMetadata, otherMetadata);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisMetadata"/> is reference-equal or value-equal to
        /// <paramref name="otherMetadata"/>.
        /// </summary>
        /// <param name="thisMetadata">An instance of <see cref="RolloutMetadata"/>. </param>
        /// <param name="otherMetadata">Another instance of <see cref="RolloutMetadata"/>. </param>
        public static bool Equals(RolloutMetadata thisMetadata, RolloutMetadata otherMetadata)
        {
            if (ReferenceEquals(thisMetadata, otherMetadata))
            {
                return true;
            }

            if (ReferenceEquals(thisMetadata, null) || ReferenceEquals(otherMetadata, null))
            {
                return false;
            }

            return thisMetadata.Name == otherMetadata.Name &&
                   thisMetadata.RolloutType == otherMetadata.RolloutType;
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// <paramref name="otherMetadata"/>.
        /// </summary>
        /// <param name="otherMetadata">Another instance of <see cref="RolloutMetadata"/>. </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#",
            Justification = "'otherMetadata' is more relevant than 'obj'. ")]
        public override bool Equals(object otherMetadata)
        {
            return Equals(this, otherMetadata as RolloutMetadata);
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
                RolloutType.GetHashCode();
        }
    }

    /// <summary>
    /// The contract for specifying the rollout policy to use for a rollout.
    /// </summary>
    public class RolloutPolicyReference
    {
        /// <summary>
        /// Gets or sets the policy to use.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the policy version that should be used.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Get string representation name for object
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return $"{nameof(RolloutPolicyReference)}: 'Name: {Name}, Version: {Version}'";
        }
    }
}