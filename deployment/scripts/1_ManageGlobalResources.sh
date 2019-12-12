#!/bin/bash
# Stop on error.
set -e
currentScriptName=`basename "$0"`

./ExecuteDeploymentRunner.sh \
--ProvisionAction="CreateOrUpdateGlobal" \
--EnvName="$APP_ASPNETCORE_ENVIRONMENT" \
--Region="$REGION"

./ImportDependencyImages.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId"

echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"