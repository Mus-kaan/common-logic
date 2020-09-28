//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Azure;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.Monitoring.Whale
{
    /// <summary>
    /// Holder for the list of resource types that can be monitored
    /// (types that have diagnostic settings and can stream data to event hubs).
    /// </summary>
    public static class SupportedResourceTypes
    {
        /// <summary>
        /// List of resource types that have diagnostic settings, obtained from
        /// https://docs.microsoft.com/en-us/azure/azure-monitor/platform/diagnostic-logs-schema .
        /// </summary>
        /// <returns></returns>
        public static readonly IEnumerable<string> s_resourceTypes = new List<string>()
        {
                    "microsoft.aad/domainservices",
                    "microsoft.analysisservices/servers",
                    "microsoft.apimanagement/service",
                    "microsoft.appplatform/spring",
                    "microsoft.automation/automationaccounts",
                    "microsoft.batch/batchaccounts",
                    "microsoft.batchai/workspaces",
                    "microsoft.blockchain/blockchainmembers",
                    "microsoft.cdn/profiles/endpoints",
                    "microsoft.classicnetwork/networksecuritygroups",
                    "microsoft.cognitiveservices/accounts",
                    "microsoft.containerregistry/registries",
                    "microsoft.containerservice/managedclusters",
                    "microsoft.databricks/workspaces",
                    "microsoft.datacatalog/datacatalogs",
                    "microsoft.datafactory/factories",
                    "microsoft.datalakeanalytics/accounts",
                    "microsoft.datalakestore/accounts",
                    "microsoft.datashare/accounts",
                    "microsoft.dbformysql/servers",
                    "microsoft.dbforpostgresql/servers",
                    "microsoft.dbforpostgresql/serversv2",
                    "microsoft.desktopvirtualization/applicationgroups",
                    "microsoft.desktopvirtualization/hostpools",
                    "microsoft.desktopvirtualization/workspaces",
                    "microsoft.devices/iothubs",
                    "microsoft.devices/provisioningservices",
                    "microsoft.documentdb/databaseaccounts",
                    "microsoft.enterpriseknowledgegraph/services",
                    "microsoft.eventhub/namespaces",
                    "microsoft.healthcareapis/services",
                    "microsoft.insights/autoscalesettings",
                    "microsoft.iotspaces/graph",
                    "microsoft.keyvault/vaults",
                    "microsoft.kusto/clusters",
                    "microsoft.logic/integrationaccounts",
                    "microsoft.logic/workflows",
                    "microsoft.machinelearningservices/workspaces",
                    "microsoft.media/mediaservices",
                    "microsoft.network/applicationgateways",
                    "microsoft.network/azurefirewalls",
                    "microsoft.network/bastionhosts",
                    "microsoft.network/expressroutecircuits",
                    "microsoft.network/frontdoors",
                    "microsoft.network/loadbalancers",
                    "microsoft.network/networksecuritygroups",
                    "microsoft.network/p2svpngateways",
                    "microsoft.network/publicipaddresses",
                    "microsoft.network/trafficmanagerprofiles",
                    "microsoft.network/virtualnetworkgateways",
                    "microsoft.network/virtualnetworks",
                    "microsoft.network/vpngateways",
                    "microsoft.powerbidedicated/capacities",
                    "microsoft.recoveryservices/vaults",
                    "microsoft.search/searchservices",
                    "microsoft.servicebus/namespaces",
                    "microsoft.sql/managedinstances",
                    "microsoft.sql/managedinstances/databases",
                    "microsoft.sql/servers/databases",
                    "microsoft.streamanalytics/streamingjobs",
                    "microsoft.web/hostingenvironments",
                    "microsoft.web/sites",
                    "microsoft.web/sites/slots",
        };

        public static void AddSupportedResourceTypes(List<string> resourcetypesTobeAdded)
        {
            s_resourceTypes.Concat(resourcetypesTobeAdded);
        }
    }
}
