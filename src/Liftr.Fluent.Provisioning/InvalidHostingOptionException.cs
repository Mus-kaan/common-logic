//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class InvalidHostingOptionException : Exception
    {
        private const string c_detailsLink = " Hosting option details: https://aka.ms/liftr/hosting-options";

        public InvalidHostingOptionException()
            : base(c_detailsLink)
        {
        }

        public InvalidHostingOptionException(string message)
            : base(message + c_detailsLink)
        {
        }

        public InvalidHostingOptionException(string message, Exception innerException)
            : base(message + c_detailsLink, innerException)
        {
        }
    }
}
