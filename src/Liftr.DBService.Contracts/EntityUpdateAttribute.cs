//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.DBService.Contracts
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class EntityUpdateAttribute : Attribute
    {
        public EntityUpdateAttribute(bool allowed)
        {
            Allowed = allowed;
        }

        public bool Allowed { get; }
    }
}