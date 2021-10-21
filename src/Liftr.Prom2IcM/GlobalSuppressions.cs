//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "<Pending>", Scope = "member", Target = "~P:Microsoft.Liftr.Prom2IcM.WebhookMessage.ExternalURL")]
[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "<Pending>", Scope = "member", Target = "~P:Microsoft.Liftr.Prom2IcM.Alert.GeneratorURL")]
[assembly: SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.Prom2IcM.Examples.WebhookMessageExample.GetExamples~Microsoft.Liftr.Prom2IcM.WebhookMessage")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.Prom2IcM.IncidentMessageGenerator.GenerateIncidentFromPrometheusAlert(Microsoft.Liftr.Prom2IcM.WebhookMessage,Microsoft.Liftr.Prom2IcM.Alert,Microsoft.Liftr.Utilities.MetaInfo,Microsoft.Liftr.Prom2IcM.ICMClientOptions,Serilog.ILogger)~Microsoft.AzureAd.Icm.Types.AlertSourceIncident")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.Prom2IcM.IncidentMessageGenerator.GenerateXHtmlDescription(Microsoft.Liftr.Prom2IcM.Alert,Microsoft.Liftr.Utilities.ComputeMetadata,System.String,System.String,System.String,System.DateTime)~Microsoft.AzureAd.Icm.Types.DescriptionEntry")]
[assembly: SuppressMessage("Reliability", "Liftr1004:Avoid calling System.Threading.Tasks.Task<TResult>.Result", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.Prom2IcM.IncidentMessageGenerator.GenerateXHtmlDescription(Microsoft.Liftr.Prom2IcM.Alert,Microsoft.Liftr.Utilities.ComputeMetadata,System.String,System.String,System.String,System.DateTime)~Microsoft.AzureAd.Icm.Types.DescriptionEntry")]
