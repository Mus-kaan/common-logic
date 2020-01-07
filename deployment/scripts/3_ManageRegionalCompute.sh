#!/bin/bash
# Stop on error.
set -e
currentScriptName=`basename "$0"`

if [ "$gcs_region" = "" ]; then
    echo "Please set GCS region using variable 'gcs_region' ..."
    exit 1 # terminate and indicate error
fi

if [ "$GenevaParametersFile" = "" ]; then
    echo "Please set the file path to the Geneva parameters using variable 'GenevaParametersFile' ..."
    exit 1 # terminate and indicate error
fi

echo "GenevaParametersFile: $GenevaParametersFile"

./ExecuteDeploymentRunner.sh \
--ProvisionAction="CreateOrUpdateRegionalCompute" \
--EnvName="$APP_ASPNETCORE_ENVIRONMENT" \
--Region="$REGION"

if [ "$DeploymentSubscriptionId" = "" ]; then
echo "Read DeploymentSubscriptionId from file 'bin/subscription-id.txt'."
DeploymentSubscriptionId=$(<bin/subscription-id.txt)
    if [ "$DeploymentSubscriptionId" = "" ]; then
        echo "Please set 'DeploymentSubscriptionId' ..."
        exit 1 # terminate and indicate error
    fi
fi

./AzLogin.sh

./DeployAKSPodIdentity.sh \
--environmentName="$APP_ASPNETCORE_ENVIRONMENT" \
--compactRegion="$compactRegion" \
--DeploymentSubscriptionId="$DeploymentSubscriptionId"

./DeployGenevaMonitoring.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId" \
--GenevaParametersFile="$GenevaParametersFile" \
--environmentName="$APP_ASPNETCORE_ENVIRONMENT" \
--compactRegion="$compactRegion" \
--Region="$REGION" \
--gcs_region="$gcs_region"

echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"