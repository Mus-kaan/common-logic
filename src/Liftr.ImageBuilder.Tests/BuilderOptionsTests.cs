//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Contracts;
using System.IO;
using Xunit;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class BuilderOptionsTests
    {
        private const string c_expectedSerilized = "{\"tenant\":\"AME\",\"subscriptionId\":\"03b9236f-5849-43b4-8c67-4c4d5235dc10\",\"location\":\"westus\",\"resourceGroupName\":\"test-rg\",\"imageGalleryName\":\"test-sig\",\"imageVersionRetentionTimeInDays\":15,\"imageReplicationRegions\":[\"westus\",\"westus2\"],\"regionalReplicaCount\":1,\"artifactStoreOptions\":{\"containerName\":\"artifact-store\",\"sasttlInMinutes\":60.0,\"oldArtifactTTLInDays\":7.0}}";
        private const string c_windowsTemplate = "{\r\n  \"$schema\": \"https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#\",\r\n  \"contentVersion\": \"1.0.0.0\",\r\n  \"resources\": [\r\n    {\r\n      \"type\": \"Microsoft.VirtualMachineImages/imageTemplates\",\r\n      \"apiVersion\": \"2019-05-01-preview\",\r\n      \"name\": \"imgtemplatename\",\r\n      \"location\": \"eastasia\",\r\n      \"dependsOn\": [],\r\n      \"properties\": {\r\n        \"source\": {\r\n          \"type\": \"PlatformImage\",\r\n          \"publisher\": \"MicrosoftWindowsServer\",\r\n          \"offer\": \"WindowsServer\",\r\n          \"sku\": \"2019-Datacenter\",\r\n          \"version\": \"2019.0.20190214\"\r\n        },\r\n        \"customize\": [\r\n          {\r\n            \"type\": \"PowerShell\",\r\n            \"name\": \"CustomizeWindowsVm\",\r\n            \"inline\": [\r\n              \"mkdir c:\\\\packer-tmp\",\r\n              \"mkdir c:\\\\packer-tmp\\\\packer-files\",\r\n              \"cd c:\\\\packer-tmp\",\r\n              \"Invoke-WebRequest 'fakeSAStoken' -OutFile c:\\\\packer-tmp\\\\packer-files.tar.gz\",\r\n              \"tar -C 'c:\\\\packer-tmp' -zxvf c:\\\\packer-tmp\\\\packer-files.tar.gz\",\r\n              \"cd c:\\\\packer-tmp\\\\packer-files\",\r\n              \".\\\\bakeImage.ps1\",\r\n              \"cd c:\\\\packer-tmp\",\r\n              \"rm packer-files.tar.gz\",\r\n              \"rm packer-files -Recurse -Force\"\r\n            ]\r\n          }\r\n        ],\r\n        \"distribute\": [\r\n          {\r\n            \"type\": \"SharedImage\",\r\n            \"galleryImageId\": \"/subscriptions/03b9236f-5849-43b4-8c67-4c4d5235dc10/resourceGroups/test-rg/providers/Microsoft.Compute/galleries/test-sig/images/testwindowsimg\",\r\n            \"runOutputName\": \"liftr-windows-base-image\",\r\n            \"artifactTags\": {\r\n              \"source\": \"azureVmImageBuilder\",\r\n              \"baseosimg\": \"windows2019\",\r\n              \"TemplateCreationTime\": \"2019-01-20T08:00:00.0000000Z\",\r\n              \"FirstCreatedAt\": \"2019-01-20T08:00:00.0000000Z\",\r\n              \"src_type\": \"PlatformImage\",\r\n              \"src_publisher\": \"MicrosoftWindowsServer\",\r\n              \"src_offer\": \"WindowsServer\",\r\n              \"src_sku\": \"2019-Datacenter\",\r\n              \"src_version\": \"2019.0.20190214\"\r\n            },\r\n            \"replicationRegions\": [\r\n              \"westus\",\r\n              \"westus2\"\r\n            ]\r\n          }\r\n        ]\r\n      }\r\n    }\r\n  ]\r\n}";
        private const string c_linuxTemplate = "{\r\n  \"$schema\": \"https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#\",\r\n  \"contentVersion\": \"1.0.0.0\",\r\n  \"resources\": [\r\n    {\r\n      \"type\": \"Microsoft.VirtualMachineImages/imageTemplates\",\r\n      \"apiVersion\": \"2019-05-01-preview\",\r\n      \"name\": \"imgtemplatename\",\r\n      \"location\": \"eastasia\",\r\n      \"dependsOn\": [],\r\n      \"properties\": {\r\n        \"source\": {\r\n          \"type\": \"SharedImageVersion\",\r\n          \"imageVersionId\": \"fake-img-r-id\"\r\n        },\r\n        \"customize\": [\r\n          {\r\n            \"type\": \"Shell\",\r\n            \"name\": \"CustomizeLiftrSettings\",\r\n            \"inline\": [\r\n              \"echo 'packer' | echo 'liftr: sudo mkdir -p /home/packer-tmp'\",\r\n              \"sudo mkdir -p /home/packer-tmp\",\r\n              \"echo 'packer' | echo 'liftr: download packer.tar'\",\r\n              \"sudo wget -O /home/packer-tmp/packer.tar 'fakeSAStoken'\",\r\n              \"echo 'packer' | echo 'liftr: sudo ls /home/packer-tmp'\",\r\n              \"sudo ls /home/packer-tmp\",\r\n              \"echo 'packer' | echo 'liftr: sudo mkdir -p /home/packer'\",\r\n              \"sudo mkdir -p /home/packer\",\r\n              \"echo 'packer' | echo 'liftr: sudo tar'\",\r\n              \"sudo tar -C /home/packer -xvf /home/packer-tmp/packer.tar\",\r\n              \"echo 'packer' | echo 'liftr: sudo rm -rf /home/packer-tmp'\",\r\n              \"sudo rm -rf /home/packer-tmp\",\r\n              \"echo 'packer' | echo 'liftr: cd /home/packer'\",\r\n              \"cd /home/packer\",\r\n              \"echo 'packer' | echo 'liftr: sudo chmod u+x *.sh'\",\r\n              \"sudo chmod u+x *.sh\",\r\n              \"echo 'packer' | echo 'run liftr bake-image.sh'\",\r\n              \"echo 'packer' | sudo -S bash -c './bake-image.sh'\"\r\n            ]\r\n          }\r\n        ],\r\n        \"distribute\": [\r\n          {\r\n            \"type\": \"SharedImage\",\r\n            \"galleryImageId\": \"/subscriptions/03b9236f-5849-43b4-8c67-4c4d5235dc10/resourceGroups/test-rg/providers/Microsoft.Compute/galleries/test-sig/images/testwindowsimg\",\r\n            \"runOutputName\": \"liftr-linux-base-image\",\r\n            \"artifactTags\": {\r\n              \"source\": \"azureVmImageBuilder\",\r\n              \"baseOS\": \"ubuntu1804\",\r\n              \"srcSBIVersion\": \"0.23677.19267\",\r\n              \"TemplateCreationTime\": \"2019-01-20T08:00:00.0000000Z\",\r\n              \"FirstCreatedAt\": \"2019-01-20T08:00:00.0000000Z\",\r\n              \"src_type\": \"SharedImageVersion\",\r\n              \"src_imageVersionId\": \"fake-img-r-id\"\r\n            },\r\n            \"replicationRegions\": [\r\n              \"westus\",\r\n              \"westus2\"\r\n            ]\r\n          }\r\n        ]\r\n      }\r\n    }\r\n  ]\r\n}";

        [Fact]
        public void SerilizationTest()
        {
            var parsed = File.ReadAllText("TestBuilderOptions.json").FromJson<BuilderOptions>();
            var serilized = parsed.ToJsonString();
            var serilized2 = serilized.FromJson<BuilderOptions>().ToJsonString();

            Assert.Equal(c_expectedSerilized, serilized2);
            Assert.Equal(serilized, serilized2);
        }

        [Fact]
        public void VerifyGenerateWindowTemplate()
        {
            var options = File.ReadAllText("TestBuilderOptions.json").FromJson<BuilderOptions>();

            var helper = new AzureImageBuilderTemplateHelper(options, new MockTimeSource());
            var id = new PlatformImageIdentifier()
            {
                Publisher = "MicrosoftWindowsServer",
                Offer = "WindowsServer",
                Sku = "2019-Datacenter",
                Version = "2019.0.20190214",
            };

            var templateContent = helper.GenerateWinodwsImageTemplate(
                Region.AsiaEast,
                "imgtemplatename",
                "testwindowsimg",
                "fakeSAStoken",
                id);

            Assert.Equal(c_windowsTemplate, templateContent);
        }

        [Fact]
        public void VerifyGenerateLinuxTemplate()
        {
            var options = File.ReadAllText("TestBuilderOptions.json").FromJson<BuilderOptions>();

            var helper = new AzureImageBuilderTemplateHelper(options, new MockTimeSource());

            var templateContent = helper.GenerateLinuxImageTemplate(
                Region.AsiaEast,
                "imgtemplatename",
                "testwindowsimg",
                "fakeSAStoken",
                "fake-img-r-id");

            Assert.Equal(c_linuxTemplate, templateContent);
        }
    }
}
