//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;

namespace Microsoft.Liftr.Logging
{
    public interface ITimedOperation : IDisposable
    {
        string Name { get; }

        /// <summary>
        /// Gets the total elapsed time measured by the current instance, in milliseconds.
        /// </summary>
        long ElapsedMilliseconds { get; }

        bool IsSuccessful { get; }

        string CallerFilePath { get; }

        string CallerMemberName { get; }

        int CallerLineNumber { get; }

        Dictionary<string, string> MetricLabels { get; }

        void SetEnvironmentType(string environmentType);

        /// <summary>
        /// Add a property to the <see cref="ITimedOperation"/>.
        /// This property will be logged in the 'operation finish' event.
        /// If you also want to log the property in all the log events in the scope of this <see cref="ITimedOperation"/>,
        /// please use <see cref="SetContextProperty(string, string)"/> instead.
        /// </summary>
        void SetProperty(string name, string value);

        /// <summary>
        /// Add a property to the <see cref="ITimedOperation"/>.
        /// This property will be logged in the 'operation finish' event.
        /// If you also want to log the property in all the log events in the scope of this <see cref="ITimedOperation"/>,
        /// please use <see cref="SetContextProperty(string, int)"/> instead.
        /// </summary>
        void SetProperty(string name, int value);

        /// <summary>
        /// Add a property to the <see cref="ITimedOperation"/>.
        /// This property will be logged in the 'operation finish' event.
        /// If you also want to log the property in all the log events in the scope of this <see cref="ITimedOperation"/>,
        /// please use <see cref="SetContextProperty(string, double)"/> instead.
        /// </summary>
        void SetProperty(string name, double value);

        /// <summary>
        /// Add a property to the <see cref="ITimedOperation"/> and all the log events under its scope.
        /// This property will be logged in the 'operation finish' event and all the log events in scope.
        /// </summary>
        void SetContextProperty(string name, string value);

        /// <summary>
        /// Add a property to the <see cref="ITimedOperation"/> and all the log events under its scope.
        /// This property will be logged in the 'operation finish' event and all the log events in scope.
        /// </summary>
        void SetContextProperty(string name, int value);

        /// <summary>
        /// Add a property to the <see cref="ITimedOperation"/> and all the log events under its scope.
        /// This property will be logged in the 'operation finish' event and all the log events in scope.
        /// </summary>
        void SetContextProperty(string name, double value);

        /// <summary>
        /// Please DO NOT use this for high cardinality values.
        /// Add a property and a metric label to the <see cref="ITimedOperation"/> and all the log events under its scope.
        /// This property will be logged in the 'operation finish' event and all the log events in scope.
        /// </summary>
        void WithLabel(string name, string value, bool setContextProperty = true);

        /// <summary>
        /// Set the 'ResultDescription' property of the <see cref="ITimedOperation"/>.
        /// </summary>
        void SetResultDescription(string resultDescription);

        /// <summary>
        /// Set the 'ResultDescription' property of the <see cref="ITimedOperation"/>.
        /// </summary>
        void SetResult(int statusCode, string resultDescription = null);

        /// <summary>
        /// Mark the <see cref="ITimedOperation"/> as failed and set the 'FailureMessage' property.
        /// </summary>
        void FailOperation(string message = null);

        /// <summary>
        /// Mark the <see cref="ITimedOperation"/> as failed and set the 'FailureStatusCode' 'FailureMessage' property.
        /// </summary>
        void FailOperation(HttpStatusCode statusCode, string message = null);
    }
}
