//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ACIS.Relay
{
    public class ACISOperationStorageOptions
    {
        public string StorageAccountConnectionString { get; set; }

        public string OperationStatusTableName { get; set; } = "acisoperationstatus";

        public string OperationNotificationQueueName { get; set; } = "acisnotification";
    }
}
