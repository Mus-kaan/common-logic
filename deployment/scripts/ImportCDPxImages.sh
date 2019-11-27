#!/bin/bash

# Stop on error.
set -e

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
    --ImageMetadataPath=*)
    ImageMetadataPath="${i#*=}"
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

if [ -z ${ImageMetadataPath+x} ]; then
    ImageMetadataPath="image-meta.json"
fi

if [ -z ${DeploymentSubscriptionId+x} ]; then
    echo "DeploymentSubscriptionId is blank."
    exit 1
fi

echo "az login --identity"
az login --identity
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "az login failed."
    exit $exit_code
fi

echo "az account set -s $DeploymentSubscriptionId"
az account set -s "$DeploymentSubscriptionId"
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "az account set failed."
    exit $exit_code
fi

set +e
# Get the name of the docker image that is tagged with the build number.
if [ -f "$ImageMetadataPath" ]; then
    echo "Using docker build metadata at '$ImageMetadataPath'."

    # Use grep magic to parse JSON since jq isn't installed on the CDPx build image.
    # See https://aka.ms/cdpx/yaml/dockerbuildcommand for the metadata file schema.
    DockerImageNameWithRegistry=$(cat $ImageMetadataPath | grep -Po '"ame_build_image_name": "\K[^"]*')

    DockerRegistry=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 | cut -d '.' -f1)
    DockerImageName=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 --complement)

    echo "DockerRegistry: $DockerRegistry"
    echo "DockerImageName: $DockerImageName"

    echo "import $DockerImageName"
    az acr import --name "$ACRName" --source $DockerImageName --registry /subscriptions/e9d570ed-cf13-4347-83e3-3938b8d65a41/resourceGroups/cdpx-acr-ame-wus/providers/Microsoft.ContainerRegistry/registries/$DockerRegistry
else
    echo "No docker build metadata file found at '$ImageMetadataPath'. Docker image name will not be injected into the Helm chart."
    exit 1
fi

exit 0