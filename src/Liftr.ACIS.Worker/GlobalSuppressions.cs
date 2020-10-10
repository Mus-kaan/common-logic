﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Reliability", "Liftr1004:Avoid calling System.Threading.Tasks.Task<TResult>.Result", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.ACIS.Worker.ACISWorker.#ctor(Microsoft.Liftr.ACIS.Worker.IACISOperationProcessor,Microsoft.Liftr.Contracts.ITimeSource,Microsoft.Liftr.Queue.IQueueReader,Microsoft.Liftr.ACIS.Relay.IACISOperationStatusEntityDataSource,Serilog.ILogger)")]
