//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.ImageBuilder
{
    public sealed class InvalidArtifactPackageException : Exception
    {
        private const string c_message = "The artifact package is in an invalid format. Please see this for more details: https://aka.ms/liftr/img-customize";

        public InvalidArtifactPackageException()
            : base(c_message)
        {
        }

        public InvalidArtifactPackageException(string message)
            : base(message + c_message)
        {
        }

        public InvalidArtifactPackageException(string message, Exception innerException)
            : base(message + c_message, innerException)
        {
        }
    }
}
