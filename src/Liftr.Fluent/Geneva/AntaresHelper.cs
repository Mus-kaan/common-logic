//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts.Geneva;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Microsoft.Liftr.Fluent.Geneva
{
    public static class AntaresHelper
    {
        private const string c_configJsonTemplatePath = "Geneva\\GenevaAntaresConfigJson.json";
        private const string c_antMdsConfigTemplatePath = "Geneva\\AntMDSConfigTemplate.json";
        private const string c_antMdsXMLConfigTemplatePath = "Geneva\\AntMDSConfigXMLTemplate.json";
        private const string c_GCSCertTemplatePath = "Geneva\\GCSCertTemplate.json";
        private const string c_GCSCertPSWDTemplatePath = "Geneva\\GCSCertPSWDTemplate.json";

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
            genevaOptions.CheckValid();
            if (!File.Exists(c_configJsonTemplatePath))
            {
                throw new InvalidOperationException($"Cannot find the template file at path: {c_configJsonTemplatePath}");
            }

            var templateContent = File.ReadAllText(c_configJsonTemplatePath);

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
            if (!File.Exists(c_antMdsConfigTemplatePath))
            {
                throw new InvalidOperationException($"Cannot find the template file at path: {c_antMdsConfigTemplatePath}");
            }

            var templateContent = File.ReadAllText(c_antMdsConfigTemplatePath);
            dynamic configObj = JObject.Parse(templateContent);
            var r = configObj.resources[0];
            r.name = $"{appServicePlanName}/AntMDS/ConfigJson";
            r.location = location.ToString();
            r.properties.settingValue = configContent;
            return JsonConvert.SerializeObject(configObj, Formatting.Indented);
        }

        public static string GenerateAntXMLConfigTemplate(Region location, string appServicePlanName)
        {
            if (!File.Exists(c_antMdsXMLConfigTemplatePath))
            {
                throw new InvalidOperationException($"Cannot find the template file at path: {c_antMdsXMLConfigTemplatePath}");
            }

            var templateContent = File.ReadAllText(c_antMdsXMLConfigTemplatePath);
            dynamic configObj = JObject.Parse(templateContent);
            var r = configObj.resources[0];
            r.name = $"{appServicePlanName}/AntMDS/MdsConfigXml";
            r.location = location.ToString();
            return JsonConvert.SerializeObject(configObj, Formatting.Indented);
        }

        public static string GenerateGCSCertTemplate(Region location, string appServicePlanName, string based64EncodedPFX)
        {
            if (!File.Exists(c_GCSCertTemplatePath))
            {
                throw new InvalidOperationException($"Cannot find the template file at path: {c_GCSCertTemplatePath}");
            }

            var templateContent = File.ReadAllText(c_GCSCertTemplatePath);
            dynamic obj = JObject.Parse(templateContent);
            var r = obj.resources[0];
            r.name = $"{appServicePlanName}/AntMDS/CERTIFICATE_PFX_GENEVACERT";
            r.location = location.ToString();
            r.properties.settingValue = based64EncodedPFX;
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public static string GenerateGCSCertPSWDTemplate(Region location, string appServicePlanName)
        {
            if (!File.Exists(c_GCSCertPSWDTemplatePath))
            {
                throw new InvalidOperationException($"Cannot find the template file at path: {c_GCSCertPSWDTemplatePath}");
            }

            var templateContent = File.ReadAllText(c_GCSCertPSWDTemplatePath);
            dynamic obj = JObject.Parse(templateContent);
            var r = obj.resources[0];
            r.name = $"{appServicePlanName}/AntMDS/CERTIFICATE_PASSWORD_GENEVACERT";
            r.location = location.ToString();
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
