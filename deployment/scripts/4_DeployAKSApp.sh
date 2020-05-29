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

AKSAppChartPackage="liftr-*.tgz"

echo "Get Key Vault endpoint and save on disk."
./ExecuteDeploymentRunner.sh \
--ProvisionAction="PrepareK8SAppDeployment" \
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

./ConnectAKS.sh

./ImportCDPxImages.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId"

./DeployAKSApp.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId" \
--compactRegion="$compactRegion" \
--APP_ASPNETCORE_ENVIRONMENT="$APP_ASPNETCORE_ENVIRONMENT" \
--AKSAppChartPackage="$AKSAppChartPackage"

./ExecuteDeploymentRunner.sh \
--ProvisionAction="UpdateAKSPublicIpInTrafficManager" \
--EnvName="$APP_ASPNETCORE_ENVIRONMENT" \
--Region="$REGION"

for script in "$CurrentDir"/4_*.sh
do
  if [[ "$script" != *"$currentScriptName"* ]]; then
    echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~"
    echo "Executing extension script '$script' :"
    $script
    echo "Finished Executing extension script '$script'."
    echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~"
  fi
done

if [ "$NoCleanUp" = "" ]; then
  rm -f *.pfx
  rm -f *.key
  rm -f *.cer
  rm -f bin/*.txt
  rm -f thanos-storage-config.yaml
  rm -f *splited-temp-cert*
fi

echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"