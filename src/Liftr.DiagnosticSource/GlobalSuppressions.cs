//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.AspNetCore")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.AspNetCore.Tests")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.GenericHosting")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Queue")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Tests.Common")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Tests.Utilities")]

[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Put the long static list at the end.", Scope = "member", Target = "~F:Microsoft.Liftr.DiagnosticSource.HttpCoreDiagnosticSourceListener.s_domainsToAddCorrelationHeader")]
