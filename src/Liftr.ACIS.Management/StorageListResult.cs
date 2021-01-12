//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using System.Collections.Generic;

namespace Microsoft.Liftr.ACIS.Management
{
    public class StorageListResult
    {
        public IEnumerable<IStorageEntity> PrimaryStorageList { get; set; }

        public IEnumerable<IStorageEntity> BackupStorageList { get; set; }
    }
}
