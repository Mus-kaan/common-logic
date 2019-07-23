//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Geneva;
using Xunit;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class AntaresHelperTests
    {
        private const string c_str1 = "{\r\n  \"MONITORING_TENANT\": \"GSMonAntares\",\r\n  \"MONITORING_ROLE\": \"Template\",\r\n  \"MONITORING_XSTORE_ACCOUNTS\": \"GCSPlaceholder\",\r\n  \"AdditionalEnvironmentVariables\": [\r\n    {\r\n      \"Key\": \"DATACENTER\",\r\n      \"Value\": \"centralus\"\r\n    },\r\n    {\r\n      \"Key\": \"MONITORING_GCS_ENVIRONMENT\",\r\n      \"Value\": \"Test\"\r\n    },\r\n    {\r\n      \"Key\": \"MONITORING_GCS_ACCOUNT\",\r\n      \"Value\": \"gcs-account1\"\r\n    },\r\n    {\r\n      \"Key\": \"MONITORING_GCS_NAMESPACE\",\r\n      \"Value\": \"namespace-asd\"\r\n    },\r\n    {\r\n      \"Key\": \"MONITORING_GCS_REGION\",\r\n      \"Value\": \"centralus\"\r\n    },\r\n    {\r\n      \"Key\": \"MONITORING_GCS_AUTH_ID\",\r\n      \"Value\": \"WEB.GENEVA.KEYVAULT.GENEVAAPP.AZUREWEBSITES.NET\"\r\n    },\r\n    {\r\n      \"Key\": \"MONITORING_GCS_AUTH_ID_TYPE\",\r\n      \"Value\": \"AuthKeyVault\"\r\n    },\r\n    {\r\n      \"Key\": \"MONITORING_CONFIG_VERSION\",\r\n      \"Value\": \"1.1\"\r\n    },\r\n    {\r\n      \"Key\": \"MONITORING_USE_GENEVA_CONFIG_SERVICE\",\r\n      \"Value\": \"true\"\r\n    }\r\n\r\n  ]\r\n\r\n}";

        [Fact]
        public void AssembleJsonConfig()
        {
            var json = AntaresHelper.GenerateAntJsonConfigTemplate(Region.UKSouth, "app-plan-name1", c_str1);
        }

        [Fact]
        public void AssembleXMLConfig()
        {
            var json = AntaresHelper.GenerateAntXMLConfigTemplate(Region.UKSouth, "app-plan-name1");
        }

        [Fact]
        public void AssembleGCSCert()
        {
            var json = AntaresHelper.GenerateGCSCertTemplate(Region.UKSouth, "app-plan-name1", "dummycontent");
        }

        [Fact]
        public void AssembleGCSCertPSWD()
        {
            var json = AntaresHelper.GenerateGCSCertPSWDTemplate(Region.UKSouth, "app-plan-name1");
        }
    }
}
