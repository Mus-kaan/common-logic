//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    /// <summary>
    /// Partner States that can be processed by the RP worker.
    /// </summary>
    public enum PartnerStatesEnum
    {
        PartnerSignup,

        AddPartnerEntity,

        LinkOrg,

        DeleteOrg,
    }
}
