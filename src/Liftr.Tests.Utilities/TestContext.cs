//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Reflection;
using Xunit.Sdk;

namespace Microsoft.Liftr.Tests.Utilities
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    /// <summary>
    ///  https://github.com/SimonCropp/XunitContext/blob/master/src/XunitContext/Context.cs
    /// </summary>
    internal class TestContext
    {
        internal Exception? _exception;

        internal TestContext(string sourceFile)
        {
            SourceFile = sourceFile;
        }

        internal TestContext()
        {
        }

        /// <summary>
        /// The source file that the current test exists in.
        ///
        /// </summary>
        public string SourceFile { get; internal set; } = null!;

        /// <summary>
        /// The <see cref="_exception"/> for the current test if it failed.
        /// </summary>
        public Exception? TestException
        {
            get
            {
                if (_exception == null)
                {
                    return null;
                }

                if (_exception is XunitException)
                {
                    return _exception;
                }

                var outerTrace = new StackTrace(_exception, false);
                var firstFrame = outerTrace.GetFrame(outerTrace.FrameCount - 1);
                var firstMethod = firstFrame.GetMethod();

                var root = firstMethod.DeclaringType.DeclaringType;
                if (root != null && root == typeof(ExceptionAggregator))
                {
                    if (_exception is TargetInvocationException targetInvocationException)
                    {
                        return targetInvocationException.InnerException;
                    }

                    return _exception;
                }

                return null;
            }
        }
    }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
}