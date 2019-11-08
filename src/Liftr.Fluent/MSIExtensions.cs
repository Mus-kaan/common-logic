//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Msi.Fluent;
using System;

namespace Microsoft.Liftr
{
    public static class MSIExtensions
    {
        public static string GetObjectId(this IIdentity msi)
        {
            if (msi == null)
            {
                throw new ArgumentNullException(nameof(msi));
            }

            return msi.Inner.PrincipalId.Value.ToString();
        }
    }
}
