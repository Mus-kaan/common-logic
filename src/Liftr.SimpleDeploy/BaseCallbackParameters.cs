//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;

namespace Microsoft.Liftr.SimpleDeploy
{
    public abstract class BaseCallbackParameters
    {
        public SimpleDeployConfigurations CallbackConfigurations { get; set; }

        public string BaseName { get; set; }

        public NamingContext NamingContext { get; set; }

        public IPPoolManager IPPoolManager { get; set; }
    }
}
