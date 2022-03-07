#!/bin/bash
# Stop on error.
set -e
CurrentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
currentScriptName=`basename "$0"`

echo "CurrentDir: $CurrentDir"
echo "currentScriptName: $currentScriptName"

if [ "$APP_ASPNETCORE_ENVIRONMENT" = "" ]; then
APP_ASPNETCORE_ENVIRONMENT="Production"
fi

echo "Export global ACR information."
./ExecuteDeploymentRunner.sh \
--ProvisionAction="ExportACRInformation" \
--EnvName="$APP_ASPNETCORE_ENVIRONMENT" \
--Region="Global"

if [ "$DeploymentSubscriptionId" = "" ]; then
DeploymentSubscriptionId=$(<bin/subscription-id.txt)
    if [ "$DeploymentSubscriptionId" = "" ]; then
        echo "Please set 'DeploymentSubscriptionId' ..."
        exit 1 # terminate and indicate error
    fi
fi

./AzLogin.sh

CraneFile="crane"
#If crane is in EV2 tar, this came from a onebranch build
if [ -f "$CraneFile" ]; then
    ./PushImageToACR.sh
else
    ./ImportCDPxImages.sh \
    --DeploymentSubscriptionId="$DeploymentSubscriptionId"
fi

echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"