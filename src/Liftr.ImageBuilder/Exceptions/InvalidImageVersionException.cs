//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.ImageBuilder
{
    public sealed class InvalidImageVersionException : Exception
    {
        private const string c_message = "The image version should follow Major(int).Minor(int).Patch(int) format. For example: 0.0.1, 1.5.13";

        public InvalidImageVersionException()
            : base(c_message)
        {
        }

        public InvalidImageVersionException(string message)
            : base(message + c_message)
        {
        }

        public InvalidImageVersionException(string message, Exception innerException)
            : base(message + c_message, innerException)
        {
        }
    }
}
