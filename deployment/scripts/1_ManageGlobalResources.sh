#!/bin/bash
# Stop on error.
set -e
currentScriptName=`basename "$0"`

./ExecuteDeploymentRunner.sh \
--ProvisionAction="OutputSubscriptionId" \
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

./RegisterFeatureAndProvider.sh --DeploymentSubscriptionId="$DeploymentSubscriptionId"

./ExecuteDeploymentRunner.sh \
--ProvisionAction="CreateOrUpdateGlobal" \
--EnvName="$APP_ASPNETCORE_ENVIRONMENT" \
--Region="$REGION"

./ImportDependencyImages.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId"

echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"