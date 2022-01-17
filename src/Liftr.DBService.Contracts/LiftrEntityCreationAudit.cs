//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.DBService.Contracts
{
    public class LiftrEntityCreationAudit : EntityWFAudit<EntityWFStages, LiftrResourceEntity>
    {
        public LiftrEntityCreationAudit(
            string resourceId,
            string tenantId,
            EntityWFStages stage,
            LiftrResourceEntity resource)
            : base(resourceId, stage, resource)
        {
            TenantId = tenantId;
        }

        public string TenantId { get; set; }
    }
}