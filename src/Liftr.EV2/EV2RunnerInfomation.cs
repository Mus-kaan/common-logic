//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.EV2
{
    public class EV2RunnerInfomation
    {
        public string Location { get; set; }

        public Guid SubscriptionId { get; set; }

        public Guid UserAssignedManagedIdentityObjectId { get; set; }

        public string UserAssignedManagedIdentityResourceId { get; set; }

        public void CheckValid()
        {
            if (SubscriptionId.Equals(Guid.Empty))
            {
                var ex = new InvalidOperationException($"Please make sure '{nameof(SubscriptionId)}' in the '{nameof(EV2RunnerInfomation)}' section is not empty.");
                throw ex;
            }

            if (UserAssignedManagedIdentityObjectId.Equals(Guid.Empty))
            {
                var ex = new InvalidOperationException($"Please make sure '{nameof(SubscriptionId)}' in the '{nameof(EV2RunnerInfomation)}' section is not empty.");
                throw ex;
            }
        }
    }
}
