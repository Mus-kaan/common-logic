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
    internal partial class TestContext
    {
        /// <summary>
        /// The source file that the current test exists in.
        /// https://github.com/SimonCropp/XunitContext/blob/master/src/XunitContext/Context.cs
        /// </summary>
        public string SourceFile { get; internal set; } = null!;

        internal Exception? Exception;

        /// <summary>
        /// The <see cref="Exception"/> for the current test if it failed.
        /// </summary>
        public Exception? TestException
        {
            get
            {
                if (Exception == null)
                {
                    return null;
                }

                if (Exception is XunitException)
                {
                    return Exception;
                }
                var outerTrace = new StackTrace(Exception, false);
                var firstFrame = outerTrace.GetFrame(outerTrace.FrameCount - 1);
                var firstMethod = firstFrame.GetMethod();

                var root = firstMethod.DeclaringType.DeclaringType;
                if (root != null && root == typeof(ExceptionAggregator))
                {
                    if (Exception is TargetInvocationException targetInvocationException)
                    {
                        return targetInvocationException.InnerException;
                    }
                    return Exception;
                }

                return null;
            }
        }

        internal TestContext(string sourceFile)
        {
            SourceFile = sourceFile;
        }

        internal TestContext()
        {
        }
    }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
}