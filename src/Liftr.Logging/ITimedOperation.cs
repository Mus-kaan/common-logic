﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Logging
{
    public interface ITimedOperation : IDisposable
    {
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
        /// Set the 'ResultDescription' property of the <see cref="ITimedOperation"/>.
        /// </summary>
        void SetResultDescription(string resultDescription);

        /// <summary>
        /// Mark the <see cref="ITimedOperation"/> as failed and set the 'FailureMessage' property.
        /// </summary>
        void FailOperation(string message = null);
    }
}
