//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Liftr
{
    public static class TypeExtensions
    {
        public static IEnumerable<FieldInfo> GetConstants(this Type type)
        {
            // Gets all public and static fields and tells it to get the fields from all base types as well.
            var fieldInfos = type?.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy) ?? throw new ArgumentNullException(nameof(type));

            // Go through the list and only pick out the constants
            // IsLiteral determines if its value is written at
            //   compile time and not changeable
            // IsInitOnly determines if the field can be set
            //   in the body of the constructor
            // for C# a field which is readonly keyword would have both true
            //   but a const field would have only IsLiteral equal to true
            return fieldInfos.Where(fi => fi.IsLiteral && !fi.IsInitOnly);
        }

        public static IEnumerable<T> GetConstantsValues<T>(this Type type)
        {
            return type.GetConstants()
                .Where(fi => fi.FieldType == typeof(T))
                .Select(x => (T)x.GetRawConstantValue());
        }
    }
}
