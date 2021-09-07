//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Notifications.Interfaces
{
    public interface INotificationManager
    {
        Task ProcessNotificationAsync(string diagnosticSettingsId, string monitorId, string tenantId, string operationType);
    }
}