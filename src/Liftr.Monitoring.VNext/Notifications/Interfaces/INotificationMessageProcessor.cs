//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Notifications.Interfaces
{
    public interface INotificationMessageProcessor
    {
        Task ProcessNotificationQueueMessageAsync(DiagnosticSettingsNotification dsNotification);
    }
}