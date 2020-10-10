//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using System;

namespace Microsoft.Liftr.ACIS.Management
{
    public class EventHubRecord : IEventHubEntity
    {
        public EventHubRecord(IEventHubEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            DocumentObjectId = entity.DocumentObjectId;
            ResourceProvider = entity.ResourceProvider;
            Namespace = entity.Namespace;
            Name = entity.Name;
            Location = entity.Location;
            AuthorizationRuleId = entity.AuthorizationRuleId;
            CreatedAtUTC = entity.CreatedAtUTC;
            IngestionEnabled = entity.IngestionEnabled;
            Active = entity.Active;
        }

        public string DocumentObjectId { get; set; }

        public MonitoringResourceProvider ResourceProvider { get; set; }

        public string Namespace { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public string EventHubConnectionString { get; set; }

        public string StorageConnectionString { get; set; }

        public string AuthorizationRuleId { get; set; }

        public DateTime CreatedAtUTC { get; set; }

        public bool IngestionEnabled { get; set; }

        public bool Active { get; set; }
    }
}
