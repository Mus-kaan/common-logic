//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// Extension Item class
    /// </summary>
    public class ExtensionItem
    {
        /// <summary>
        /// Gets or sets Extension Type
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Property names are chosen for brevity.")]
        public string Type { get; set; }

        /// <summary>
        /// Gets the properties
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisItem"/> is reference-equal or value-equal to
        /// <paramref name="otherItem"/>.
        /// </summary>
        /// <param name="thisItem">An instance of <see cref="ExtensionItem"/>. </param>
        /// <param name="otherItem">Another instance of <see cref="ExtensionItem"/>. </param>
        public static bool operator ==(ExtensionItem thisItem, ExtensionItem otherItem)
        {
            return Equals(thisItem, otherItem);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisItem"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherItem"/>.
        /// </summary>
        /// <param name="thisItem">An instance of <see cref="ExtensionItem"/>. </param>
        /// <param name="otherItem">Another instance of <see cref="ExtensionItem"/>. </param>
        public static bool operator !=(ExtensionItem thisItem, ExtensionItem otherItem)
        {
            return !Equals(thisItem, otherItem);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisItem"/> is reference-equal or value-equal to
        /// <paramref name="otherItem"/>.
        /// </summary>
        /// <param name="thisItem">An instance of <see cref="ExtensionItem"/>. </param>
        /// <param name="otherItem">Another instance of <see cref="ExtensionItem"/>. </param>
        public static bool Equals(ExtensionItem thisItem, ExtensionItem otherItem)
        {
            if (object.ReferenceEquals(thisItem, otherItem))
            {
                return true;
            }

            if (object.ReferenceEquals(thisItem, null) || object.ReferenceEquals(otherItem, null))
            {
                return false;
            }

            return string.Equals(thisItem.Type, otherItem.Type, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// </summary>
        /// <param name="obj">Other Rollout action</param>
        /// <returns>if the current instance is reference-equal or value-equal to <paramref name="obj"/></returns>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as ExtensionItem);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            return Type.ToUpper(CultureInfo.InvariantCulture).GetHashCode();
        }
    }
}
