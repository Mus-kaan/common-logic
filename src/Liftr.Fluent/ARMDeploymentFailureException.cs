//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Fluent
{
    public class ARMDeploymentFailureException : Exception
    {
        public ARMDeploymentFailureException()
        {
        }

        public ARMDeploymentFailureException(string message)
            : base(message)
        {
        }

        public ARMDeploymentFailureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DeploymentError Details { get; set; }
    }
}
