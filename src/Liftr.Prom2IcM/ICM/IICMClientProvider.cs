//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AzureAd.Icm.Types;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Prom2IcM
{
    public interface IICMClientProvider
    {
        Task<ITaskBasedConnector> GetICMClientAsync();
    }
}
