//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.ImageBuilder
{
    public class DuplicatedSharedImageGalleryImagerVersionException : Exception
    {
        public DuplicatedSharedImageGalleryImagerVersionException()
        {
        }

        public DuplicatedSharedImageGalleryImagerVersionException(string message)
            : base(message)
        {
        }

        public DuplicatedSharedImageGalleryImagerVersionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
