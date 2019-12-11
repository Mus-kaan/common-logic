//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// Email Notification model class
    /// </summary>
    public class EmailNotification
    {
        /// <summary>
        /// Gets or sets To Address list separator with ",;"
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// Gets or sets Cc Address list separator with ",;"
        /// </summary>
        public string CC { get; set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisNotification"/> is reference-equal or value-equal to
        /// <paramref name="otherNotification"/>.
        /// </summary>
        /// <param name="thisNotification">An instance of <see cref="EmailNotification"/>. </param>
        /// <param name="otherNotification">Another instance of <see cref="EmailNotification"/>. </param>
        public static bool operator ==(EmailNotification thisNotification, EmailNotification otherNotification)
        {
            return Equals(thisNotification, otherNotification);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisNotification"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherNotification"/>.
        /// </summary>
        /// <param name="thisNotification">An instance of <see cref="RolloutNotification"/>. </param>
        /// <param name="otherNotification">Another instance of <see cref="RolloutNotification"/>. </param>
        public static bool operator !=(EmailNotification thisNotification, EmailNotification otherNotification)
        {
            return !Equals(thisNotification, otherNotification);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisNotification"/> is reference-equal or value-equal to
        /// <paramref name="otherNotification"/>.
        /// </summary>
        /// <param name="thisNotification">An instance of <see cref="EmailNotification"/>. </param>
        /// <param name="otherNotification">Another instance of <see cref="EmailNotification"/>. </param>
        public static bool Equals(EmailNotification thisNotification, EmailNotification otherNotification)
        {
            if (ReferenceEquals(thisNotification, otherNotification))
            {
                return true;
            }

            if (ReferenceEquals(thisNotification, null) || ReferenceEquals(otherNotification, null))
            {
                return false;
            }

            return thisNotification.To.Equals(otherNotification.To, StringComparison.OrdinalIgnoreCase) &&
                   thisNotification.CC.Equals(otherNotification.CC, StringComparison.OrdinalIgnoreCase);
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
            return Equals(this, otherNotification as EmailNotification);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            return To.GetHashCode();
        }
    }
}
