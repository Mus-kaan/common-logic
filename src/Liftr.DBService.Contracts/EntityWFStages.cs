//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.DBService.Contracts
{
    public enum EntityWFStages
    {
        CreateMarketplaceResource = 0,
        ActivateMarketplaceOffer = 1,
        AddPartnerEntity = 2,
        AddMarketplaceRelationshipEntity = 3,
        AddMessageToWhale = 4,
        Successful = 5,
        Failed = 6,
        CleanUp = 7,
        CreateIngestionAPIKey = 8,
        ConfigureDeployment = 9,
        FinalCleanUp = 10,
        CreateWorkflowMax = 11, // insert new create flow state above this.
        /* Create workflow End */

        // insert new delete flow state above "DeleteWorkflowMax".
        /* Delete workflow Start */
        SendDeleteResourceMessageToWhale = 101,
        DeleteMarketplaceRelationshipEntity = 102,
        ResetMonitorStatusEntity = 103,
        Deleted = 104,
        DeleteWorkflowMax = 105, // insert new delete flow state above this.
        /* Delete workflow End */
    }
}