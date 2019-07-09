//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Logging
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DoNotLogAttribute : Attribute
    {
    }
}
