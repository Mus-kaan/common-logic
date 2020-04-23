//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;

namespace Microsoft.Liftr.SimpleDeploy
{
    public abstract class BaseCallbackParameters
    {
        public SimpleDeployConfigurations CallbackConfigurations { get; set; }

        public string BaseName { get; set; }

        public NamingContext NamingContext { get; set; }
    }
}
