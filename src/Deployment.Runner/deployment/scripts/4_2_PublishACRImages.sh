#!/bin/bash
# Stop on error.
set -e
CurrentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# https://portal.azure.com/?feature.customportal=false#@microsoft.onmicrosoft.com/resource/subscriptions/af20b225-d632-4791-97a0-2b33fa486420/resourceGroups/public-ms-acr-rg/providers/Microsoft.ContainerRegistry/registries/liftracr/overview
ACRName="liftracr"
echo "ACRName: $ACRName"
az account set -s af20b225-d632-4791-97a0-2b33fa486420

if [ "$TenantId" = "" ]; then
echo "Read TenantId from file 'bin/tenant-id.txt'."
TenantId=$(<bin/tenant-id.txt)
    if [ "$TenantId" = "" ]; then
        echo "Please set 'TenantId' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "TenantId: $TenantId"

if [ -z ${ImageMetadataDir+x} ]; then
    ImageMetadataDir="$CurrentDir/cdpx-images"
fi
echo "CDPx images meta data folder: $ImageMetadataDir"

if [ "$TenantId" == "72f988bf-86f1-41af-91ab-2d7cd011db47" ]; then
    echo "Microsoft Tenant"
    CDPXACRResourceId="/subscriptions/fb0fee9c-b18a-4d61-887c-eb59b04a2b02/resourceGroups/cxprod-acr/providers/Microsoft.ContainerRegistry/registries/cdpxlinux"
else
    echo "AME Tenant. We only push to the MS tenant ACR."
    exit 0
fi

echo "Start importing docker images from CDPx's ACR. The access need to be granted for the EV2 MI."
echo "Please get the EV2 MI's object Id and use it in the 'ACR self-serve' page. The ACR we need access is 'cdpxlinux' for Microsoft tenant, 'cdpxlinuxame' for AME tenant."
echo "Details: https://onebranch.visualstudio.com/Pipeline/_wiki/wikis/Pipeline.wiki/1909/Self-serve-ACR-Access"

imgMetaData=$ImageMetadataDir/prom2icm.json
fileName="$(basename $imgMetaData)"
fileName=$(echo "$fileName" | cut -f 1 -d '.')
echo "Parsing image meta data file: $imgMetaData"

# Use grep magic to parse JSON since jq isn't installed on the CDPx build image.
# See https://aka.ms/cdpx/yaml/dockerbuildcommand for the metadata file schema.

DockerImageNameWithRegistry=$(cat $imgMetaData | grep -Po '"build_image_name": "\K[^"]*')
DockerRegistry=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 | cut -d '.' -f1)
DockerImageName=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 --complement)
parts=(${DockerImageName//:/ })
Repository=${parts[0]}
Tag=${parts[1]}

echo "DockerRegistry: $DockerRegistry"
echo "DockerImageName: $DockerImageName"
echo "Repository: $Repository"
echo "Tag: $Tag"

echo "Check existing tags for repository $Repository"
set +e
existingTags=$(az acr repository show-tags --name $ACRName --repository $Repository)
set -e

if echo "$existingTags" | grep $Tag; then
    echo "'$DockerImageName' already exist, skip import."
else
    echo "az acr import --name $ACRName --source $DockerImageName --registry $CDPXACRResourceId --force"
    az acr import --name "$ACRName" --source $DockerImageName --registry "$CDPXACRResourceId" --force
fi