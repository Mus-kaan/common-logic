//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts.Geneva;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.Liftr.Fluent.Geneva
{
    public static class AntaresHelper
    {
        private const string c_placeHolder_MONITORING_TENANT = "PLACEHOLDER_MONITORING_TENANT";
        private const string c_placeHolder_MONITORING_ROLE = "PLACEHOLDER_MONITORING_ROLE";
        private const string c_placeHolder_REGION = "PLACEHOLDER_REGION";
        private const string c_placeHolder_MONITORING_GCS_ENVIRONMENT = "PLACEHOLDER_MONITORING_GCS_ENVIRONMENT";
        private const string c_placeHolder_MONITORING_GCS_ACCOUNT = "PLACEHOLDER_MONITORING_GCS_ACCOUNT";
        private const string c_placeHolder_MONITORING_GCS_NAMESPACE = "PLACEHOLDER_MONITORING_GCS_NAMESPACE";
        private const string c_placeHolder_MONITORING_GCS_AUTH_ID = "PLACEHOLDER_MONITORING_GCS_AUTH_ID";
        private const string c_placeHolder_MONITORING_CONFIG_VERSION = "PLACEHOLDER_MONITORING_CONFIG_VERSION";

        public static string AssembleConfigJson(GenevaOptions genevaOptions, Region location)
        {
            if (genevaOptions == null)
            {
                throw new ArgumentNullException(nameof(genevaOptions));
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            genevaOptions.CheckValid();

            var templateContent = EmbeddedContentReader.GetContent("Microsoft.Liftr.Fluent.Geneva.GenevaAntaresConfigJson.json");

            templateContent = templateContent.Replace(c_placeHolder_MONITORING_TENANT, genevaOptions.MonitoringTenant);
            templateContent = templateContent.Replace(c_placeHolder_MONITORING_ROLE, genevaOptions.MonitoringRole);
            templateContent = templateContent.Replace(c_placeHolder_REGION, location.ToString());
            templateContent = templateContent.Replace(c_placeHolder_MONITORING_GCS_ENVIRONMENT, genevaOptions.MonitoringGCSEnvironment);
            templateContent = templateContent.Replace(c_placeHolder_MONITORING_GCS_ACCOUNT, genevaOptions.MonitoringGCSAccount);
            templateContent = templateContent.Replace(c_placeHolder_MONITORING_GCS_NAMESPACE, genevaOptions.MonitoringGCSNamespace);
            templateContent = templateContent.Replace(c_placeHolder_MONITORING_GCS_AUTH_ID, genevaOptions.MonitoringGCSClientCertificateSAN);
            templateContent = templateContent.Replace(c_placeHolder_MONITORING_CONFIG_VERSION, genevaOptions.MonitoringConfigVersion);

            return templateContent;
        }

        public static string GenerateAntJsonConfigTemplate(Region location, string appServicePlanName, string configContent)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var templateContent = EmbeddedContentReader.GetContent("Microsoft.Liftr.Fluent.Geneva.AntMDSConfigTemplate.json");
            dynamic configObj = JObject.Parse(templateContent);
            var r = configObj.resources[0];
            r.name = $"{appServicePlanName}/AntMDS/ConfigJson";
            r.location = location.ToString();
            r.properties.settingValue = configContent;
            return JsonConvert.SerializeObject(configObj, Formatting.Indented);
        }

        public static string GenerateAntXMLConfigTemplate(Region location, string appServicePlanName)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var templateContent = EmbeddedContentReader.GetContent("Microsoft.Liftr.Fluent.Geneva.AntMDSConfigXMLTemplate.json");
            dynamic configObj = JObject.Parse(templateContent);
            var r = configObj.resources[0];
            r.name = $"{appServicePlanName}/AntMDS/MdsConfigXml";
            r.location = location.ToString();
            return JsonConvert.SerializeObject(configObj, Formatting.Indented);
        }

        public static string GenerateGCSCertTemplate(Region location, string appServicePlanName, string based64EncodedPFX)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var templateContent = EmbeddedContentReader.GetContent("Microsoft.Liftr.Fluent.Geneva.GCSCertTemplate.json");
            dynamic obj = JObject.Parse(templateContent);
            var r = obj.resources[0];
            r.name = $"{appServicePlanName}/AntMDS/CERTIFICATE_PFX_GENEVACERT";
            r.location = location.ToString();
            r.properties.settingValue = based64EncodedPFX;
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public static string GenerateGCSCertPSWDTemplate(Region location, string appServicePlanName)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var templateContent = EmbeddedContentReader.GetContent("Microsoft.Liftr.Fluent.Geneva.GCSCertPSWDTemplate.json");
            dynamic obj = JObject.Parse(templateContent);
            var r = obj.resources[0];
            r.name = $"{appServicePlanName}/AntMDS/CERTIFICATE_PASSWORD_GENEVACERT";
            r.location = location.ToString();
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
