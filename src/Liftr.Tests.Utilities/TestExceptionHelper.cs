//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Liftr.Tests.Utilities
{
    /// <summary>
    /// https://github.com/SimonCropp/XunitContext/blob/master/src/XunitContext/XunitContext.cs
    /// </summary>
    internal static class TestExceptionHelper
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        static AsyncLocal<TestContext?> local = new AsyncLocal<TestContext?>();
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        static bool enableExceptionCapture;

        public static void EnableExceptionCapture()
        {
            if (enableExceptionCapture)
            {
                return;
            }

            enableExceptionCapture = true;
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                if (local.Value == null)
                {
                    return;
                }

                local.Value.Exception = e.Exception;
            };
        }

        /// <summary>
        /// The <see cref="Exception"/> for the current test if it failed.
        /// </summary>
        public static Exception TestException => local.Value?.TestException;

        public static TestContext Register([CallerFilePath] string sourceFile = "")
        {
            var existingContext = local.Value;

            if (existingContext == null)
            {
                var context = new TestContext(sourceFile);
                local.Value = context;
                return context;
            }

            existingContext.SourceFile = sourceFile;
            return existingContext;
        }
    }
}
