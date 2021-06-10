//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Microsoft.Liftr.ManagedIdentity
{
    [Serializable]
    public class MSIClientException : Exception
    {
        protected MSIClientException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            OperationName = info.GetString(nameof(OperationName));
            IsRetryable = info.GetBoolean(nameof(IsRetryable));
        }

        private MSIClientException(string message, bool isRetryable, string operationName, Exception innerException = null)
            : base(message, innerException)
        {
            OperationName = operationName;
            IsRetryable = isRetryable;
        }

        public string OperationName { get; }

        public bool IsRetryable { get; }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(OperationName), OperationName);
            info.AddValue(nameof(IsRetryable), IsRetryable);
            base.GetObjectData(info, context);
        }

        public static MSIClientException IdentityClientException(
            string message,
            Exception innerException = null,
            [CallerMemberName]string operationName = "")
        {
            return new MSIClientException(
                message: message,
                isRetryable: false,
                innerException: innerException,
                operationName: operationName);
        }

        public static MSIClientException IdentityServiceException(
            string message,
            Exception innerException = null,
            [CallerMemberName]string operationName = "")
        {
            return new MSIClientException(
                message: message,
                isRetryable: true,
                innerException: innerException,
                operationName: operationName);
        }
    }
}
