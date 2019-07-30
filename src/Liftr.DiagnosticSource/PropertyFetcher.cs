//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.Liftr.DiagnosticSource
{
    /// <summary>
    /// Efficient implementation of fetching properties of anonymous types with reflection.
    /// https://github.com/microsoft/ApplicationInsights-dotnet-server/blob/18a6a0a2a2b5fc069f1c855431973f837bfb4b4d/Src/Common/PropertyFetcher.cs
    /// https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/HttpDiagnosticsGuide.md
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "This is used substantially")]
    internal class PropertyFetcher
    {
        private readonly string _propertyName;
        private PropertyFetch _innerFetcher;

        public PropertyFetcher(string propertyName)
        {
            _propertyName = propertyName;
        }

        public object Fetch(object obj)
        {
            if (_innerFetcher == null)
            {
                _innerFetcher = PropertyFetch.FetcherForProperty(obj.GetType().GetTypeInfo().GetDeclaredProperty(_propertyName));
            }

            return _innerFetcher?.Fetch(obj);
        }

        // see https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs
        private class PropertyFetch
        {
            /// <summary>
            /// Create a property fetcher from a .NET Reflection PropertyInfo class that
            /// represents a property of a particular type.
            /// </summary>
            public static PropertyFetch FetcherForProperty(PropertyInfo propertyInfo)
            {
                if (propertyInfo == null)
                {
                    // returns null on any fetch.
                    return new PropertyFetch();
                }

                var typedPropertyFetcher = typeof(TypedFetchProperty<,>);
                var instantiatedTypedPropertyFetcher = typedPropertyFetcher.GetTypeInfo().MakeGenericType(
                    propertyInfo.DeclaringType, propertyInfo.PropertyType);
                return (PropertyFetch)Activator.CreateInstance(instantiatedTypedPropertyFetcher, propertyInfo);
            }

            /// <summary>
            /// Given an object, fetch the property that this propertyFetch represents.
            /// </summary>
            public virtual object Fetch(object obj)
            {
                return null;
            }

            private class TypedFetchProperty<TObject, TProperty> : PropertyFetch
            {
                private readonly Func<TObject, TProperty> _propertyFetch;

                public TypedFetchProperty(PropertyInfo property)
                {
                    _propertyFetch = (Func<TObject, TProperty>)property.GetMethod.CreateDelegate(typeof(Func<TObject, TProperty>));
                }

                public override object Fetch(object obj)
                {
                    return _propertyFetch((TObject)obj);
                }
            }
        }
    }
}
