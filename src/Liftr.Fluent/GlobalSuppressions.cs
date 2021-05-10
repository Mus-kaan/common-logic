//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Liftr.Fluent.ResourceProviderRegister.s_commonProviderList")]
[assembly: SuppressMessage("Reliability", "Liftr1005:Avoid calling System.Threading.Tasks.Task.Wait()", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.CosmosDBOpenNetworkScope.Dispose")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.Fluent.StorageAccountCredentialLifeCycleManager.GetActiveConnectionStringAsync~System.Threading.Tasks.Task{System.String}")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Liftr.Fluent.CredentialLifeCycleManager._timeSource")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Liftr.Fluent.CredentialLifeCycleManager._logger")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Liftr.Fluent.CredentialLifeCycleManager._rotateAfterDays")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.Fluent.CredentialLifeCycleManager.GetCurrentStateFromTags(System.Collections.Generic.IReadOnlyDictionary{System.String,System.String})~Microsoft.Liftr.Fluent.ActiveCredentailTagState")]
