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

imageVersionTag=$(<semantic.fileversion.info)

./RunImageBuilder.sh \
--ConfigurationPath="$ConfigurationPath" \
--ImageName="$ImageName" \
--ImageVersionTag="$imageVersionTag" \
--SourceImage="$SourceImage" \
--RunnerSPNObjectId="$RunnerSPNObjectId" \
--OnlyOutputSubscriptionId="true"

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
--ImageVersionTag="$imageVersionTag" \
--SourceImage="$SourceImage" \
--RunnerSPNObjectId="$RunnerSPNObjectId" \
--OnlyOutputSubscriptionId="false"

echo "----------------------------------------------------------------------------------------------"
echo "Finished running the Liftr Image Builder."
echo "----------------------------------------------------------------------------------------------"
echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"