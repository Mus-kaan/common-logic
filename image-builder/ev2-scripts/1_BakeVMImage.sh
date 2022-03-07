#!/bin/bash
# Stop on error.
set -e

CurrentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
echo "CurrentDir: $CurrentDir"

if [ "$SourceImage" = "" ]; then
    echo "Please set the source image type using variable 'SourceImage' ..."
    exit 1 # terminate and indicate error
fi

if [ "$ImageName" = "" ]; then
    echo "Please set the image name using variable 'ImageName' ..."
    exit 1 # terminate and indicate error
fi

if [ "$ConfigurationPath" = "" ]; then
    echo "Please set a path to the config file using variable 'ConfigurationPath' ..."
    exit 1 # terminate and indicate error
fi

if [ "$RunnerSPNObjectId" = "" ]; then
    echo "Please set the object Id of the executing service principal using variable 'RunnerSPNObjectId' ..."
    exit 1 # terminate and indicate error
fi

if [ "$Cloud" = "" ]; then
    echo "Please set the cloud type using variable 'Cloud' ..."
    exit 1 # terminate and indicate error
fi

# CDPx Versioning: https://onebranch.visualstudio.com/Pipeline/_wiki/wikis/Pipeline.wiki/325/Versioning
ImageVersion=$(<numeric.packageversion.info)

CraneFile="crane"

#If crane is in EV2 tar, this came from a onebranch build
if [ -f "$CraneFile" ]; then
    ImageMetadataDir="$CurrentDir/docker-image-metadata"
else
    ImageMetadataDir="$CurrentDir/cdpx-images"
fi

if [ -d $ImageMetadataDir ]; then
    echo "=============================================================================================="
    echo "Found container image metadata directory. Start importing container images first."
    echo "=============================================================================================="

    ./RunImageBuilder.sh \
    --ConfigurationPath="$ConfigurationPath" \
    --ImageName="$ImageName" \
    --ImageVersion="$ImageVersion" \
    --SourceImage="$SourceImage" \
    --RunnerSPNObjectId="$RunnerSPNObjectId" \
    --OnlyOutputACR="true" \
    --Cloud="$Cloud"

    echo "Read ACRName from file 'bin/acr-name.txt'."
    ACRName=$(<bin/acr-name.txt)
    if [ "$ACRName" = "" ]; then
        echo "Cannot find ACRName ..."
        exit 1 # terminate and indicate error
    fi
    echo "ACRName: $ACRName"

    echo "Read DeploymentSubscriptionId from file 'bin/acr-subscription-id.txt'."
    DeploymentSubscriptionId=$(<bin/acr-subscription-id.txt)
    if [ "$DeploymentSubscriptionId" = "" ]; then
        echo "Cannot find DeploymentSubscriptionId ..."
        exit 1 # terminate and indicate error
    fi
    echo "DeploymentSubscriptionId: $DeploymentSubscriptionId"

    ./AzLogin.sh

    #If crane is in EV2 tar, this came from a onebranch build
    if [ -f "$CraneFile" ]; then
        ./ImportImagesFromTarArtifacts.sh
    else
        ./ImportCDPxImages.sh \
        --ACRName="$ACRName" \
        --ImageMetadataDir="$ImageMetadataDir" \
        --DeploymentSubscriptionId="$DeploymentSubscriptionId"
    fi

    ImportImageScript="$CurrentDir/ImportDependencyImages.sh"

    if [ -f "$ImportImageScript" ]; then
        ./ImportDependencyImages.sh \
        --ACRName="$ACRName" \
        --DeploymentSubscriptionId="$DeploymentSubscriptionId"
    fi

    echo "=============================================================================================="
    echo "Finished importing container images."
    echo "=============================================================================================="
fi

./RunImageBuilder.sh \
--ConfigurationPath="$ConfigurationPath" \
--ImageName="$ImageName" \
--ImageVersion="$ImageVersion" \
--SourceImage="$SourceImage" \
--RunnerSPNObjectId="$RunnerSPNObjectId" \
--OnlyOutputACR="false" \
--Cloud="$Cloud"

echo "----------------------------------------------------------------------------------------------"
echo "Finished running Liftr Image Builder to bake VM image."
echo "----------------------------------------------------------------------------------------------"
echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"