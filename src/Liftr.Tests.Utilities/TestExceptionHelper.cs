﻿//-----------------------------------------------------------------------------
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
        private static AsyncLocal<TestContext?> s_local = new AsyncLocal<TestContext?>();
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        private static bool s_enableExceptionCapture;

        /// <summary>
        /// The <see cref="Exception"/> for the current test if it failed.
        /// </summary>
        public static Exception TestException => s_local.Value?.TestException;

        public static void EnableExceptionCapture()
        {
            if (s_enableExceptionCapture)
            {
                return;
            }

            s_enableExceptionCapture = true;
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                if (s_local.Value == null)
                {
                    return;
                }

                s_local.Value._exception = e.Exception;
            };
        }

        public static TestContext Register([CallerFilePath] string sourceFile = "")
        {
            var existingContext = s_local.Value;

            if (existingContext == null)
            {
                var context = new TestContext(sourceFile);
                s_local.Value = context;
                return context;
            }

            existingContext.SourceFile = sourceFile;
            return existingContext;
        }
    }
}
