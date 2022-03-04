//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.EV2
{
    public abstract class BaseEV2Options
    {
        public string ServiceTreeName { get; set; }

        public Guid ServiceTreeId { get; set; }

        public string NotificationEmail { get; set; }

        public string[] OneBranchContainerImages { get; set; }
    }
}
