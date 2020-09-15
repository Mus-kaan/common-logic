//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
using System.Diagnostics.CodeAnalysis;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "Liftr1202:Type name should match file name", Justification = "Generic type naming convention", Scope = "type", Target = "~T:Microsoft.Liftr.DataSource.Mongo.ResourceEntityDataSource`1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "collection is used in derieved classes", Scope = "member", Target = "~F:Microsoft.Liftr.DataSource.Mongo.ResourceEntityDataSource`1._collection")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Liftr.DataSource.Mongo.ResourceEntityDataSource`1._timeSource")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Liftr.DataSource.Mongo.ResourceEntityDataSource`1._rateLimiter")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringBaseEntityDataSource`1._logger")]
