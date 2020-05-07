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
        private const string c_expectedSerilized = "{\"tenant\":\"AME\",\"keepAzureVMImageBuilderLogs\":false,\"subscriptionId\":\"03b9236f-5849-43b4-8c67-4c4d5235dc10\",\"location\":\"westus\",\"resourceGroupName\":\"test-rg\",\"imageGalleryName\":\"test-sig\",\"packerVMSize\":\"Standard_D2s_v3\",\"imageVersionRetentionTimeInDays\":15,\"imageReplicationRegions\":[\"westus\",\"westus2\"],\"regionalReplicaCount\":1,\"contentStoreOptions\":{\"artifactContainerName\":\"artifact-store\",\"vhdExportContainerName\":\"exporting-vhds\",\"vhdImportContainerName\":\"importing-vhds\",\"sourceSBIContainerName\":\"sbi-source-images\",\"sasttlInMinutes\":60.0,\"contentTTLInDays\":7.0,\"exportingVHDContainerSASTTLInDays\":5.0}}";
        private const string c_windowsTemplate = "{\"$schema\":\"https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#\",\"contentVersion\":\"1.0.0.0\",\"resources\":[{\"type\":\"Microsoft.VirtualMachineImages/imageTemplates\",\"apiVersion\":\"2019-05-01-preview\",\"name\":\"imgtemplatename\",\"location\":\"eastasia\",\"dependsOn\":[],\"identity\":{\"type\":\"UserAssigned\",\"userAssignedIdentities\":{\"/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/dd-dev-data20200102-eus-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/dd-dev-data20200102-eus-msi\":{}}},\"properties\":{\"vmProfile\":{\"vmSize\":\"Standard_D2s_v3\"},\"source\":{\"type\":\"PlatformImage\",\"publisher\":\"MicrosoftWindowsServer\",\"offer\":\"WindowsServer\",\"sku\":\"2019-Datacenter\",\"version\":\"2019.0.20190214\"},\"customize\":[{\"type\":\"PowerShell\",\"name\":\"CustomizeWindowsVm\",\"inline\":[\"mkdir c:\\\\packer-tmp\",\"mkdir c:\\\\packer-tmp\\\\packer-files\",\"cd c:\\\\packer-tmp\",\"Invoke-WebRequest 'fakeSAStoken' -OutFile c:\\\\packer-tmp\\\\packer-files.tar.gz\",\"tar -C 'c:\\\\packer-tmp' -zxvf c:\\\\packer-tmp\\\\packer-files.tar.gz\",\"cd c:\\\\packer-tmp\\\\packer-files\",\".\\\\bakeImage.ps1\",\"cd c:\\\\packer-tmp\",\"rm packer-files.tar.gz\",\"rm packer-files -Recurse -Force\"]}],\"distribute\":[{\"type\":\"VHD\",\"runOutputName\":\"liftr-vhd-output\"},{\"type\":\"SharedImage\",\"galleryImageId\":\"/subscriptions/03b9236f-5849-43b4-8c67-4c4d5235dc10/resourceGroups/test-rg/providers/Microsoft.Compute/galleries/test-sig/images/testwindowsimg/versions/0.24234.2781\",\"runOutputName\":\"liftr-windows-base-image\",\"artifactTags\":{\"source\":\"azureVmImageBuilder\",\"baseosimg\":\"windows2019\",\"TemplateCreationTime\":\"2019-01-20T08:00:00.0000000Z\",\"FirstCreatedAt\":\"2019-01-20T08:00:00.0000000Z\",\"src_type\":\"PlatformImage\",\"src_publisher\":\"MicrosoftWindowsServer\",\"src_offer\":\"WindowsServer\",\"src_sku\":\"2019-Datacenter\",\"src_version\":\"2019.0.20190214\"},\"replicationRegions\":[\"westus\",\"westus2\"]}]}}]}";
        private const string c_linuxTemplate = "{\"$schema\":\"https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#\",\"contentVersion\":\"1.0.0.0\",\"resources\":[{\"type\":\"Microsoft.VirtualMachineImages/imageTemplates\",\"apiVersion\":\"2019-05-01-preview\",\"name\":\"imgtemplatename\",\"location\":\"eastasia\",\"dependsOn\":[],\"identity\":{\"type\":\"UserAssigned\",\"userAssignedIdentities\":{\"/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/dd-dev-data20200102-eus-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/dd-dev-data20200102-eus-msi\":{}}},\"properties\":{\"vmProfile\":{\"vmSize\":\"Standard_D2s_v3\"},\"source\":{\"type\":\"SharedImageVersion\",\"imageVersionId\":\"fake-img-r-id\"},\"customize\":[{\"type\":\"Shell\",\"name\":\"CustomizeLiftrSettings\",\"inline\":[\"echo '[liftr-image-builder] Toubleshooting guide: https://aka.ms/liftr/aib-tsg'\",\"echo '[liftr-image-builder] sudo mkdir -p /home/packer-tmp'\",\"sudo mkdir -p /home/packer-tmp\",\"echo '[liftr-image-builder] download packer.tar'\",\"sudo wget -O /home/packer-tmp/packer.tar 'fakeSAStoken'\",\"echo '[liftr-image-builder] sudo ls /home/packer-tmp'\",\"sudo ls /home/packer-tmp\",\"echo '[liftr-image-builder] sudo mkdir -p /home/packer'\",\"sudo mkdir -p /home/packer\",\"echo '[liftr-image-builder] sudo tar'\",\"sudo tar -C /home/packer -xvf /home/packer-tmp/packer.tar\",\"echo '[liftr-image-builder] sudo rm -rf /home/packer-tmp'\",\"sudo rm -rf /home/packer-tmp\",\"echo '[liftr-image-builder] cd /home/packer'\",\"cd /home/packer\",\"echo '[liftr-image-builder] sudo chmod u+x *.sh'\",\"sudo chmod u+x *.sh\",\"echo '[liftr-image-builder] run Liftr VM Image Builder entry script: bake-image.sh'\",\"sudo -S bash -c './bake-image.sh'\"]}],\"distribute\":[{\"type\":\"VHD\",\"runOutputName\":\"liftr-vhd-output\"},{\"type\":\"SharedImage\",\"galleryImageId\":\"/subscriptions/03b9236f-5849-43b4-8c67-4c4d5235dc10/resourceGroups/test-rg/providers/Microsoft.Compute/galleries/test-sig/images/testwindowsimg/versions/0.24234.2781\",\"runOutputName\":\"liftr-linux-base-image\",\"artifactTags\":{\"source\":\"azureVmImageBuilder\",\"baseOS\":\"ubuntu1804\",\"srcSBIVersion\":\"0.23677.19267\",\"TemplateCreationTime\":\"2019-01-20T08:00:00.0000000Z\",\"FirstCreatedAt\":\"2019-01-20T08:00:00.0000000Z\",\"src_type\":\"SharedImageVersion\",\"src_imageVersionId\":\"fake-img-r-id\"},\"replicationRegions\":[\"westus\",\"westus2\"]}]}}]}";

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
                "0.24234.2781",
                "fakeSAStoken",
                "/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/dd-dev-data20200102-eus-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/dd-dev-data20200102-eus-msi",
                id,
                null,
                false);

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
                "0.24234.2781",
                "fakeSAStoken",
                "/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/dd-dev-data20200102-eus-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/dd-dev-data20200102-eus-msi",
                "fake-img-r-id",
                null,
                false);

            Assert.Equal(c_linuxTemplate, templateContent);
        }
    }
}
