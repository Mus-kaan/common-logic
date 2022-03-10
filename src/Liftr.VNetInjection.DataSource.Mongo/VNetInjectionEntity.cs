//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Microsoft.Liftr.VNetInjection.DataSource.Mongo
{
    public class VNetInjectionEntity : BaseResourceEntity
    {
        [BsonElement("frontendIPConfig")]
        public FrontendIPConfiguration FrontendIPConfiguration { get; set; }

        [BsonElement("nicConfig")]
        public NetworkInterfaceConfiguration NetworkInterfaceConfiguration { get; set; }

        [BsonElement("managedRg")]
        [BsonIgnoreIfNull]
        public string ManagedResourceGroupName { get; set; }
    }

    public class PrivateIPAddress
    {
        [BsonElement("ip")]
        public string IPAddress { get; set; }

        [BsonElement("allocationMethod")]
        [BsonRepresentation(BsonType.String)]
        public PrivateIPAllocationMethod AllocationMethod { get; set; }

        [BsonElement("subnetRid")]
        public string SubnetId { get; set; }
    }

    public class FrontendIPConfiguration
    {
        [BsonElement("publicIPRids")]
        [BsonIgnoreIfNull]
        public IEnumerable<string> PublicIPResourceIds { get; set; }

        [BsonElement("privateIPs")]
        [BsonIgnoreIfNull]
        public IEnumerable<PrivateIPAddress> PrivateIPAddresses { get; set; }
    }

    public class NetworkInterfaceConfiguration
    {
        [BsonElement("subnetRids")]
        public IEnumerable<string> DelegatedSubnetResourceIds { get; set; }
    }
}
