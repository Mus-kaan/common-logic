﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// An exception that is thrown when the validation of a request or document fails.
    /// </summary>
    [Serializable]
    public class ContractValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContractValidationException"/> class.
        /// </summary>
        public ContractValidationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractValidationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ContractValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractValidationException"/> class with a
        /// specified error message
        /// and a reference to the inner exception that is the cause of this exception.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
        /// if no inner exception is specified.</param>
        public ContractValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractValidationException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about
        /// the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about
        /// the source or destination.</param>
        protected ContractValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
