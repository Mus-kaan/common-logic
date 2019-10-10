//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Liftr.Fluent
{
    public static class EmbeddedContentReader
    {
        public static string GetContent(Assembly assembly, string contentName)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (contentName == null)
            {
                throw new InvalidOperationException("Cannot find the embedded content with name: " + contentName);
            }

            var stream = assembly.GetManifestResourceStream(contentName);
            var textStreamReader = new StreamReader(stream);
            return textStreamReader.ReadToEnd();
        }

        internal static string GetContent(string contentName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return GetContent(assembly, contentName);
        }
    }
}
