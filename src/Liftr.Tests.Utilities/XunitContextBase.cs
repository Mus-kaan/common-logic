using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Tests.Utilities
{
    public abstract class XunitContextBase :
        IDisposable
    {
        static XunitContextBase()
        {
            XunitContext.EnableExceptionCapture();
        }

        public Context Context { get; }

        protected XunitContextBase(
            ITestOutputHelper output,
            [CallerFilePath] string sourceFile = "")
        {
            Guard.AgainstNull(output, nameof(output));
            Guard.AgainstNullOrEmpty(sourceFile, nameof(sourceFile));

            Context = XunitContext.Register(output, sourceFile);
        }

        public virtual void Dispose()
        {
            Context.Flush();
        }

        /// <summary>
        /// The <see cref="Exception"/> for the current test if it failed.
        /// </summary>
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public Exception? TestException
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            get => Context.TestException;
        }

        /// <summary>
        /// The source file that the current test exists in.
        /// </summary>
        public string SourceFile
        {
            get => Context.SourceFile;
        }
    }
}