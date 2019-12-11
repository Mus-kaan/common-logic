//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr
{
    public static class EnumUtil
    {
        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static IEnumerable<string> GetValueStrings<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>().Select(i => i.ToString());
        }

        /// <summary>
        /// Checks if the given string value is a valid value in the specified enum type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The enum type.</param>
        /// <returns>True if value is present else false.</returns>
        public static bool IsValidEnumValue(string value, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.IsEnum)
            {
                throw new ArgumentException("Type 'type' must be an 'enum'.");
            }

            return Enum.GetNames(type).Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Prints the enum names separated by comma.
        /// </summary>
        /// <param name="type">Enum type</param>
        /// <returns>A string of enum names</returns>
        public static string PrintEnumNamesSeparatedByComma(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.IsEnum)
            {
                throw new ArgumentException("Type 'type' must be an 'enum'.");
            }

            var names = Enum.GetNames(type);

            return string.Join(", ", names);
        }
    }
}
