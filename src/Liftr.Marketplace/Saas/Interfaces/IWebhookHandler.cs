﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Saas.Interfaces
{
    public interface IWebhookHandler
    {
        Task<OperationUpdateStatus> ProcessDeleteAsync(WebhookPayload payload);

        Task<OperationUpdateStatus> ProcessChangePlanAsync(WebhookPayload payload);

        Task<OperationUpdateStatus> ProcessChangeQuantityAsync(WebhookPayload payload);

        Task<OperationUpdateStatus> ProcessSuspendAsync(WebhookPayload payload);

        Task<OperationUpdateStatus> ProcessReinstateAsync(WebhookPayload payload);
    }
}