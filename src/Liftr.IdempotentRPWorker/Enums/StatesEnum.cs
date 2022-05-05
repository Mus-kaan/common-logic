//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    /// <summary>
    /// States that can be processed by the RP worker.
    /// </summary>
    public enum StatesEnum
    {
        /* Initialized State */
        Intialized,

        /* Create resource states */
        CreateSaaS,

        ActivateSaaS,

        PartnerSignup,

        AddResourceEntity,

        AddPartnerEntity,

        AddMarketplaceRelationshipEntity,

        AddMessageToWhale,

        ConfigureDeployment,

        CreateDeployment,

        CreateUserAccount,

        CreateIngestionAPIKey,

        GetDeploymentStatus,

        AddLogForwarderPartnerInfoToDb,

        AddManagedIdentityInfoToDb,

        LinkOrg,

        LinkMarketplace,

        UpdateEntityInfoToDb,

        /* Delete resource states */

        DeleteSaaS,

        DeleteDeployment,

        DeleteResourceEntity,

        DeleteMarketplaceRelationshipEntity,

        ResetMonitorStatusEntity,

        SendDeleteResourceMessageToWhale,

        DeleteOrg,

        /* Custom State */
        CustomState,

        /* Terminal States */
        Terminated,
        Failed,
        Succeeded,

        WaitForCreateAck,

        CallbackNotReceivedFromPartner,

        AddTimeoutMessage,
    }
}
