//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Tests.Utilities
{
    internal static class XunitContext
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        static AsyncLocal<Context?> local = new AsyncLocal<Context?>();
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

                if (local.Value.flushed)
                {
                    return;
                }

                local.Value.Exception = e.Exception;
            };
        }

        public static Context Context
        {
            get
            {
                var context = local.Value;
                if (context != null)
                {
                    return context;
                }

                context = new Context();
                local.Value = context;
                return context;
            }
        }

        public static Context Register(
            ITestOutputHelper output,
            [CallerFilePath] string sourceFile = "")
        {
            var existingContext = local.Value;

            if (existingContext == null)
            {
                var context = new Context(output, sourceFile);
                local.Value = context;
                return context;
            }

            if (existingContext.TestOutput != null)
            {
                throw new Exception("A ITestOutputHelper has already been registered.");
            }

            existingContext.TestOutput = output;
            existingContext.SourceFile = sourceFile;
            return existingContext;
        }
    }
}