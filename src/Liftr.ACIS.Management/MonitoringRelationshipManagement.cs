//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.ACIS.Relay;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Microsoft.Liftr.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Management
{
    public sealed class MonitoringRelationshipManagement : IDisposable
    {
        private readonly IPartnerResourceDataSource<PartnerResourceEntity> _partnerResourceDataSource;
        private readonly IMonitoringRelationshipDataSource<MonitoringRelationship> _relationshipDataSource;
        private readonly TenantHelper _tenantHelper = new TenantHelper();

        public MonitoringRelationshipManagement(
            IPartnerResourceDataSource<PartnerResourceEntity> partnerResourceDataSource,
            IMonitoringRelationshipDataSource<MonitoringRelationship> relationshipDataSource)
        {
            _partnerResourceDataSource = partnerResourceDataSource;
            _relationshipDataSource = relationshipDataSource;
        }

        public void Dispose()
        {
            _tenantHelper.Dispose();
        }

        public async Task<IEnumerable<MonitoringRelationship>> ListMonitoringRelationshipAsync(IACISOperation operation, string monitorResourceId)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            var monitorId = new ResourceId(monitorResourceId);
            var tenantId = await _tenantHelper.GetTenantIdForSubscriptionAsync(monitorId.SubscriptionId);

            var partner = (await _partnerResourceDataSource.ListAsync(monitorResourceId)).FirstOrDefault();
            if (partner == null)
            {
                await operation.FailAsync($"Cannot find the partner resource with Id {monitorResourceId}");
                return null;
            }

            var relationship = await _relationshipDataSource.ListByPartnerResourceAsync(tenantId, partner.EntityId);
            await operation.SuccessfulFinishAsync(relationship.ToJson(indented: true));

            return relationship;
        }
    }
}
