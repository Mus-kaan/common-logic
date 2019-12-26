#!/bin/bash
# Stop on error.
set -e
CurrentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

for i in "$@"
do
case $i in
    --ACRName=*)
    ACRName="${i#*=}"
    shift # past argument=value
    ;;
    --DeploymentSubscriptionId=*)
    DeploymentSubscriptionId="${i#*=}"
    shift # past argument=value
    ;;
    --ImageMetadataDir=*)
    ImageMetadataDir="${i#*=}"
    shift # past argument=value
    ;;
    *)
    echo "Not matched option '${i#*=}' passed in."
    exit 1
    ;;
esac
done

if [ "$ACRName" = "" ]; then
echo "Read ACRName from file 'bin/acr-name.txt'."
ACRName=$(<bin/acr-name.txt)
    if [ "$ACRName" = "" ]; then
        echo "Please set 'ACRName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "ACRName: $ACRName"

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

if [ -z ${DeploymentSubscriptionId+x} ]; then
    echo "DeploymentSubscriptionId is blank."
    exit 1
fi

if [ "$TenantId" == "72f988bf-86f1-41af-91ab-2d7cd011db47" ]; then
    echo "Microsoft Tenant"
    CDPXACRResourceId="/subscriptions/fb0fee9c-b18a-4d61-887c-eb59b04a2b02/resourceGroups/cxprod-acr/providers/Microsoft.ContainerRegistry/registries/cdpxlinux"
else
    echo "AME Tenant"
    CDPXACRResourceId="/subscriptions/e9d570ed-cf13-4347-83e3-3938b8d65a41/resourceGroups/cdpx-acr-ame-wus/providers/Microsoft.ContainerRegistry/registries/cdpxlinuxame"
fi

for imgMetaData in $ImageMetadataDir/*.json
do
    fileName="$(basename $imgMetaData)"
    fileName=$(echo "$fileName" | cut -f 1 -d '.')
    echo "Parsing image meta data file: $imgMetaData"

    # Use grep magic to parse JSON since jq isn't installed on the CDPx build image.
    # See https://aka.ms/cdpx/yaml/dockerbuildcommand for the metadata file schema.

    if [ "$TenantId" == "72f988bf-86f1-41af-91ab-2d7cd011db47" ]; then
        DockerImageNameWithRegistry=$(cat $imgMetaData | grep -Po '"build_image_name": "\K[^"]*')
    else
        DockerImageNameWithRegistry=$(cat $imgMetaData | grep -Po '"ame_build_image_name": "\K[^"]*')
    fi

    DockerRegistry=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 | cut -d '.' -f1)
    DockerImageName=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 --complement)

    echo "DockerRegistry: $DockerRegistry"
    echo "DockerImageName: $DockerImageName"

    echo "import $DockerImageName"
    echo "az acr import --name $ACRName --source $DockerImageName --registry $CDPXACRResourceId --force"
    az acr import --name "$ACRName" --source $DockerImageName --registry "$CDPXACRResourceId" --force
done