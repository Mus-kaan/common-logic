//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.DBService.Contracts
{
    public class EntityWFAudit<TSTage, TResource> : BaseRpEntity where TSTage : Enum
    {
        public EntityWFAudit(string resourceId, TSTage stage, TResource resource)
        {
            ResourceId = resourceId;
            Stage = stage;
            PreviousStage = stage;
            Resource = resource;
        }

        public TSTage Stage { get; private set; }

        public TSTage PreviousStage { get; private set; }

        public TResource Resource { get; private set; }

        public void UpdateStage(TSTage stage)
        {
            PreviousStage = Stage;
            Stage = stage;
        }

        public void UpdateResource(TResource resource)
        {
            Resource = resource;
        }
    }
}