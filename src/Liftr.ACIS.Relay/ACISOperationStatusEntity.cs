//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.ACIS.Relay
{
    public static class ACISOperationStatusType
    {
        public const string Created = nameof(Created);

        public const string Started = nameof(Started);

        public const string Failed = nameof(Failed);

        public const string Delegated = nameof(Delegated);

        public const string Succeeded = nameof(Succeeded);
    }

    public class ACISOperationStatusEntity : TableEntity
    {
        public ACISOperationStatusEntity()
        {
        }

        public ACISOperationStatusEntity(string operationName, string operationId)
            : base(operationName, operationId)
        {
            OperationName = operationName;
            OperationId = operationId;
        }

        public string OperationName { get; set; }

        public string OperationId { get; set; }

        public string Status { get; set; } = ACISOperationStatusType.Created;

        public string Result { get; set; }

        public string Logs { get; set; }

        public bool IsFinished()
        {
            return Status == ACISOperationStatusType.Failed
                || Status == ACISOperationStatusType.Delegated
                || Status == ACISOperationStatusType.Succeeded;
        }

        public void AddLogEntry(LogEntry entry)
        {
            var entries = GetLogEntries().ToList();
            entries.Add(entry);
            Logs = entries.ToJson();
        }

        public IEnumerable<LogEntry> GetLogEntries()
        {
            List<LogEntry> entries = null;

            if (string.IsNullOrEmpty(Logs))
            {
                entries = new List<LogEntry>();
            }
            else
            {
                entries = Logs.FromJson<List<LogEntry>>();
            }

            return entries.OrderBy(e => e.TimeStamp);
        }
    }
}
