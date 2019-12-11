//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     A class that defines how the build for a particular rollout is stored and how it can be accessed.
    /// </summary>
    public class BuildSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildSource"/> class.
        /// </summary>
        public BuildSource()
        {
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     The parameters that define how to access and/or prepare the build from this build source.
        /// </summary>
        public IDictionary<string, string> Parameters { get; set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisSource"/> is reference-equal or value-equal to
        /// <paramref name="otherSource"/>.
        /// </summary>
        /// <param name="thisSource">An instance of <see cref="BuildSource"/>. </param>
        /// <param name="otherSource">Another instance of <see cref="BuildSource"/>. </param>
        public static bool operator ==(BuildSource thisSource, BuildSource otherSource)
        {
            return Equals(thisSource, otherSource);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisSource"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherSource"/>.
        /// </summary>
        /// <param name="thisSource">An instance of <see cref="BuildSource"/>. </param>
        /// <param name="otherSource">Another instance of <see cref="BuildSource"/>. </param>
        public static bool operator !=(BuildSource thisSource, BuildSource otherSource)
        {
            return !Equals(thisSource, otherSource);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisSource"/> is reference-equal or value-equal to
        /// <paramref name="otherSource"/>.
        /// </summary>
        /// <param name="thisSource">An instance of <see cref="BuildSource"/>. </param>
        /// <param name="otherSource">Another instance of <see cref="BuildSource"/>. </param>
        public static bool Equals(BuildSource thisSource, BuildSource otherSource)
        {
            if (ReferenceEquals(thisSource, otherSource))
            {
                return true;
            }

            if (ReferenceEquals(thisSource, null) || ReferenceEquals(otherSource, null))
            {
                return false;
            }

            return thisSource.Parameters.Equals(otherSource.Parameters);
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// <paramref name="otherSource"/>.
        /// </summary>
        /// <param name="otherSource">Another instance of <see cref="BuildSource"/>. </param>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#",
            Justification = "'otherSource' is more relevant than 'obj'. ")]
        public override bool Equals(object otherSource)
        {
            return Equals(this, otherSource as BuildSource);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            return Parameters.GetHashCode();
        }
    }
}