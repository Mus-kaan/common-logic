//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts.ARM
{
    public enum SubscriptionState
    {
        NotDefined,
        Registered,
        Unregistered,
        Warned,
        Suspended,
        Deleted,
    }

    public class SubscriptionNotification
    {
        public DateTime? RegistrationDate { get; set; }

        public SubscriptionState? State { get; set; }

        public SubscriptionProperties Properties { get; set; }
    }
}
