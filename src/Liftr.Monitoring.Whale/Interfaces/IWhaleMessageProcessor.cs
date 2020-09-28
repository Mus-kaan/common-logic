//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Whale.Interfaces
{
    public interface IWhaleMessageProcessor
    {
        Task ProcessUpdateTagRulesMessageAsync(string monitorId, string tenantId);

        Task<ProvisioningState> ProcessAutoMonitoringMessageAsync(string partnerEntityId, string tenantId);

        Task ProcessDeleteMessageAsync(string partnerEntityId, string tenantId);
    }
}
