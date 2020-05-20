//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.EV2
{
    public class InvalidImageBuilderEV2OptionsException : Exception
    {
        public const string c_ImageBuilderEV2Link = "https://aka.ms/liftr/img-ev2-options";
        public const string c_DetailsMessage = " See more details for the image builder EV2 options at: " + c_ImageBuilderEV2Link;

        public InvalidImageBuilderEV2OptionsException()
            : base(c_DetailsMessage)
        {
        }

        public InvalidImageBuilderEV2OptionsException(string message)
            : base(message + c_DetailsMessage)
        {
        }

        public InvalidImageBuilderEV2OptionsException(string message, Exception innerException)
            : base(message + c_DetailsMessage, innerException)
        {
        }
    }
}
