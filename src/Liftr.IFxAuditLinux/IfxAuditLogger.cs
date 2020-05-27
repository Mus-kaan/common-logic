//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.IFxAudit;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Liftr.IFxAuditLinux
{
    public interface IIfxAuditLogger
    {
        IFxAuditReturnValue LogAudit(
            ICollection<IFxAuditCallerId> callerIds,
            IFxAuditEventCategories eventCategory,
            string operationName,
            ICollection<IFxAuditTargetResource> targets,
            IFxAuditResultType result,
            string resultDescription = "");
    }

    public class IfxAuditLogger : IIfxAuditLogger
    {
        private static bool s_isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        static IfxAuditLogger()
        {
            if (!s_isLinux)
            {
                Console.WriteLine("Not running on Linux. Skipping Microsoft.IFxAudit.");
                return;
            }

            var spec = new IFxAudit.IFxAuditSpecification()
            {
                Dest = OutputDestination.STDOUT,
                HeartBeatIntervalSeconds = 60,
            };
            IFxAuditReturnValue initValue = IFxAudit.IFxAudit.Initialize(spec);

            if (initValue != IFxAuditReturnValue.AUDIT_SUCCESS)
            {
                Console.Error.WriteLine("Microsoft.IFxAudit initialization failed.");
            }
            else
            {
                Console.WriteLine("Microsoft.IFxAudit is initialized.");
            }
        }

        public IfxAuditLogger()
        {
        }

        public IFxAuditReturnValue LogAudit(
            ICollection<IFxAuditCallerId> callerIds,
            IFxAuditEventCategories eventCategory,
            string operationName,
            ICollection<IFxAuditTargetResource> targets,
            IFxAuditResultType result,
            string resultDescription = "")
        {
            if (!s_isLinux)
            {
                return IFxAuditReturnValue.AUDIT_SUCCESS;
            }

            var reqProperties = new IFxAuditRequiredProperties();
            reqProperties.CallerIds = callerIds;
            reqProperties.EventCategories = new IFxAuditEventCategories[]
            {
                eventCategory,
            };
            reqProperties.OperationName = operationName;
            reqProperties.ResultType = result;
            reqProperties.TargetResources = targets;

            var optProperties = new IFxAuditOptionalProperties();
            optProperties.ResultDescription = resultDescription;

            return IFxAudit.IFxAudit.LogAudit(AuditType.APPLICATION, reqProperties, optProperties);
        }
    }
}
