#!/bin/bash
# Stop on error.
set -e
currentScriptName=`basename "$0"`

ssh-keygen -m PEM -t rsa -b 4096 -f bin/liftr_ssh_key -N ""

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