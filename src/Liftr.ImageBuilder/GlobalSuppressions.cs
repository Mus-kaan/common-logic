//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.ImageBuilder.ContentStore.CleanUpExportingVHDsAsync~System.Threading.Tasks.Task{System.Int32}")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.ImageBuilder.ImageBuilderOrchestrator.CleanUpAsync(Microsoft.Azure.Management.Fluent.IAzure,System.String,Microsoft.Liftr.ImageBuilder.ImageGalleryClient)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.ImageBuilder.ImageBuilderOrchestrator.CheckLatestSourceSBIAndCacheLocallyAsync(System.String,Microsoft.Liftr.Contracts.SourceImageType,Microsoft.Liftr.ImageBuilder.ImageGalleryClient)~System.Threading.Tasks.Task{Microsoft.Azure.Management.Compute.Fluent.IGalleryImageVersion}")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.ImageBuilder.ImageBuilderOrchestrator.BuildCustomizedSBIAsync(System.String,System.String,Microsoft.Liftr.Contracts.SourceImageType,System.String,System.Collections.Generic.IDictionary{System.String,System.String},System.Threading.CancellationToken)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.ImageBuilder.ContentStore.CleanUpVHDsAsync(Azure.Storage.Blobs.BlobContainerClient)~System.Threading.Tasks.Task{System.Int32}")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.ImageBuilder.ImageGalleryClient.CleanUpOldImageVersionAsync(Microsoft.Azure.Management.Fluent.IAzure,System.String,System.String,System.String,System.Int32)~System.Threading.Tasks.Task{System.Int32}")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Liftr.ImageBuilder.ImageBuilderOrchestrator.GetLastRunState(System.String)~System.String")]
