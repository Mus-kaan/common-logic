#!/bin/bash
# Stop on error.
set -e

# print all commands for debug
# set -x

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
    ImageMetadataDir="$CurrentDir/docker-image-metadata"
fi
echo "Docker images meta data folder: $ImageMetadataDir"

if [ "$TenantId" == "72f988bf-86f1-41af-91ab-2d7cd011db47" ]; then
    echo "Microsoft Tenant"
else
    echo "AME Tenant. We only push to the MS tenant ACR."
    exit 0
fi

imgMetaData=$ImageMetadataDir/prom2icm-metadata.json
fileName="$(basename $imgMetaData)"
fileName=$(echo "$fileName" | cut -f 1 -d '.')
echo "Parsing image meta data file: $imgMetaData"

# Parse JSON using grep
# See https://onebranch.visualstudio.com/OneBranch/_wiki/wikis/OneBranch.wiki/4601/Build-Docker-Images?anchor=metadata for the metadata file schema.

DockerImageNameWithRegistry=$(cat $imgMetaData | grep -Po '"base_image_name": "\K[^"]*')

DockerImageName=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 --complement)

Repository=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 --complement)
DockerImageName=$(echo $Repository | cut -d '/' -f1 --complement | cut -d '/' -f1 --complement)
Tag=$(cat $imgMetaData | grep -Po '"build_tag": "\K[^"]*')

echo "DockerImageName: $DockerImageName"
echo "Repository: $Repository"
echo "Tag: $Tag"

GivenRepositoryName=$(cat $imgMetaData | grep -Po '"repository_name": "\K[^"]*')

echo "Check existing tags for repository $Repository"
set +e
existingTags=$(az acr repository show-tags --name $ACRName --repository $Repository)
set -e

Crane="./crane"

if echo "$existingTags" | grep $Tag; then
    echo "'$DockerImageName' already exist, skip import."
else
    DestImageFullName="$ACRName.azurecr.io/$Repository:$Tag"

    echo "Docker image does not exist [$Repository:$Tag]. Uploading image from tar file $DestImageFullName"
    echo "Getting ACR credentials"
    TokenQueryRes=$(az acr login -n "$ACRName" -t)
    Token=$(echo "$TokenQueryRes" | grep -Po '"accessToken": "\K[^"]*')
    DestinationACR=$(echo "$TokenQueryRes" | grep -Po '"loginServer": "\K[^"]*')
    echo $Token | $Crane auth login "$DestinationACR" -u "00000000-0000-0000-0000-000000000000" --password-stdin

    TarImagePath="$CurrentDir/docker-images/$GivenRepositoryName.tar.gz"
    UnzippedTarImagePath="$CurrentDir/docker-images/$GivenRepositoryName.tar"

    if [ -f "$UnzippedTarImagePath" ]; then
        echo "Docker push $UnzippedTarImagePath to $DestImageFullName"
        $Crane push $UnzippedTarImagePath $DestImageFullName
    else
        echo "Cannot find tar file at $UnzippedTarImagePath. Exiting..."
        exit 1
    fi
fi