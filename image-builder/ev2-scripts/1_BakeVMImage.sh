#!/bin/bash
# Stop on error.
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

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

./RunImageBuilder.sh \
--ConfigurationPath="$ConfigurationPath" \
--ImageName="$ImageName" \
--ImageVersion="$ImageVersion" \
--SourceImage="$SourceImage" \
--RunnerSPNObjectId="$RunnerSPNObjectId" \
--OnlyOutputSubscriptionId="true" \
--Cloud="$Cloud"

if [ "$DeploymentSubscriptionId" = "" ]; then
DeploymentSubscriptionId=$(<bin/subscription-id.txt)
    if [ "$DeploymentSubscriptionId" = "" ]; then
        echo "Please set 'DeploymentSubscriptionId' ..."
        exit 1 # terminate and indicate error
    fi
fi

./AzLogin.sh

./RegisterFeatureAndProvider.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId"

./RunImageBuilder.sh \
--ConfigurationPath="$ConfigurationPath" \
--ImageName="$ImageName" \
--ImageVersion="$ImageVersion" \
--SourceImage="$SourceImage" \
--RunnerSPNObjectId="$RunnerSPNObjectId" \
--OnlyOutputSubscriptionId="false" \
--Cloud="$Cloud"

echo "----------------------------------------------------------------------------------------------"
echo "Finished running Liftr Image Builder to bake VM image."
echo "----------------------------------------------------------------------------------------------"
echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"