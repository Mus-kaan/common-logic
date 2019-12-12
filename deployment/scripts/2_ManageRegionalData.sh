#!/bin/bash
# Stop on error.
set -e
currentScriptName=`basename "$0"`

if [ "$ActiveKey" = "" ]; then
    ActiveKey="Primary MongoDB Connection String"
fi

./ExecuteDeploymentRunner.sh \
--ProvisionAction="CreateOrUpdateRegionalData" \
--EnvName="$APP_ASPNETCORE_ENVIRONMENT" \
--Region="$REGION"

echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"