#!/bin/bash
# Stop on error.
set -e
CurrentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
currentScriptName=`basename "$0"`

echo "CurrentDir: $CurrentDir"
echo "currentScriptName: $currentScriptName"

ssh-keygen -m PEM -t rsa -b 4096 -f bin/liftr_ssh_key -N ""

./ExecuteDeploymentRunner.sh \
--ProvisionAction="OutputSubscriptionId" \
--EnvName="$APP_ASPNETCORE_ENVIRONMENT" \
--Region="$REGION"

if [ "$DeploymentSubscriptionId" = "" ]; then
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

for script in "$CurrentDir"/1_*.sh
do
  if [[ "$script" != *"$currentScriptName"* ]]; then
    echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~"
    echo "Executing extension script '$script' :"
    $script
    echo "Finished Executing extension script '$script'."
    echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~"
  fi
done

echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"