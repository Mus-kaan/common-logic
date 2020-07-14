//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.IFxAudit;
using System.Collections.Generic;

namespace Microsoft.Liftr.IFxAuditLinux
{
    public interface IIfxAuditLogger
    {
        IFxAuditReturnValue LogAudit(ICollection<IFxAuditCallerId> callerIds, IFxAuditEventCategories eventCategory, string operationName, ICollection<IFxAuditTargetResource> targets, IFxAuditResultType result, string resultDescription = "");

        IFxAuditReturnValue LogAudit(AuditType auditType, IFxAuditRequiredProperties rqdProps, IFxAuditOptionalProperties optProps = null, IDictionary<string, string> partC = null);
    }
}
