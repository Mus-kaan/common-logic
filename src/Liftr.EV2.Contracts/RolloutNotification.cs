//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// Rollout Notification Setting
    /// </summary>
    public class RolloutNotification
    {
        /// <summary>
        /// Gets or sets Email Notification
        /// </summary>
        public EmailNotification Email { get; set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisNotification"/> is reference-equal or value-equal to
        /// <paramref name="otherNotification"/>.
        /// </summary>
        /// <param name="thisNotification">An instance of <see cref="RolloutNotification"/>. </param>
        /// <param name="otherNotification">Another instance of <see cref="RolloutNotification"/>. </param>
        public static bool operator ==(RolloutNotification thisNotification, RolloutNotification otherNotification)
        {
            return Equals(thisNotification, otherNotification);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisNotification"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherNotification"/>.
        /// </summary>
        /// <param name="thisNotification">An instance of <see cref="RolloutNotification"/>. </param>
        /// <param name="otherNotification">Another instance of <see cref="RolloutNotification"/>. </param>
        public static bool operator !=(RolloutNotification thisNotification, RolloutNotification otherNotification)
        {
            return !Equals(thisNotification, otherNotification);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisNotification"/> is reference-equal or value-equal to
        /// <paramref name="otherNotification"/>.
        /// </summary>
        /// <param name="thisNotification">An instance of <see cref="RolloutNotification"/>. </param>
        /// <param name="otherNotification">Another instance of <see cref="RolloutNotification"/>. </param>
        public static bool Equals(RolloutNotification thisNotification, RolloutNotification otherNotification)
        {
            if (ReferenceEquals(thisNotification, otherNotification))
            {
                return true;
            }

            if (ReferenceEquals(thisNotification, null) || ReferenceEquals(otherNotification, null))
            {
                return false;
            }

            return thisNotification.Email == otherNotification.Email;
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// <paramref name="otherNotification"/>.
        /// </summary>
        /// <param name="otherNotification">Another instance of <see cref="RolloutNotification"/>. </param>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#",
            Justification = "'otherNotification' is more relevant than 'obj'. ")]
        public override bool Equals(object otherNotification)
        {
            return Equals(this, otherNotification as RolloutNotification);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            return Email.GetHashCode();
        }
    }
}
