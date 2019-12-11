//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     A class that contains information that can be used to uniquely identify an Azure service.
    /// </summary>
    public class ServiceMetadata
    {
        /// <summary>
        ///     The human-readable name of the current instance of this Azure service.
        /// </summary>
        public string ServiceGroup { get; set; }

        /// <summary>
        ///     The service identifier.
        /// </summary>
        public string ServiceIdentifier { get; set; }

        /// <summary>
        ///     The environment that this particular service is operating in.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisMetadata"/> is reference-equal or value-equal to
        /// <paramref name="otherMetadata"/>.
        /// </summary>
        /// <param name="thisMetadata">An instance of <see cref="ServiceMetadata"/>. </param>
        /// <param name="otherMetadata">Another instance of <see cref="ServiceMetadata"/>. </param>
        public static bool operator ==(ServiceMetadata thisMetadata, ServiceMetadata otherMetadata)
        {
            return Equals(thisMetadata, otherMetadata);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisMetadata"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherMetadata"/>.
        /// </summary>
        /// <param name="thisMetadata">An instance of <see cref="ServiceMetadata"/>. </param>
        /// <param name="otherMetadata">Another instance of <see cref="ServiceMetadata"/>. </param>
        public static bool operator !=(ServiceMetadata thisMetadata, ServiceMetadata otherMetadata)
        {
            return !Equals(thisMetadata, otherMetadata);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisMetadata"/> is reference-equal or value-equal to
        /// <paramref name="otherMetadata"/>.
        /// </summary>
        /// <param name="thisMetadata">An instance of <see cref="ServiceMetadata"/>. </param>
        /// <param name="otherMetadata">Another instance of <see cref="ServiceMetadata"/>. </param>
        public static bool Equals(ServiceMetadata thisMetadata, ServiceMetadata otherMetadata)
        {
            if (ReferenceEquals(thisMetadata, otherMetadata))
            {
                return true;
            }

            if (ReferenceEquals(thisMetadata, null) || ReferenceEquals(otherMetadata, null))
            {
                return false;
            }

            return thisMetadata.ServiceGroup == otherMetadata.ServiceGroup &&
                   thisMetadata.Environment == otherMetadata.Environment;
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// <paramref name="otherMetadata"/>.
        /// </summary>
        /// <param name="otherMetadata">Another instance of <see cref="ServiceMetadata"/>. </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#",
            Justification = "'otherMetadata' is more relevant than 'obj'. ")]
        public override bool Equals(object otherMetadata)
        {
            return Equals(this, otherMetadata as ServiceMetadata);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            return ServiceGroup.GetHashCode() + Environment.GetHashCode();
        }
    }
}