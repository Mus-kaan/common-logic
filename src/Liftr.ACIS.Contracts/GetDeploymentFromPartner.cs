//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ACIS.Contracts
{
    public class GetDeploymentFromPartner
    {
        /// <summary>
        /// Subscription Id
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Resource Group for the deployment(s)
        /// </summary>
        public string ResourceGroup { get; set; }
    }
}
