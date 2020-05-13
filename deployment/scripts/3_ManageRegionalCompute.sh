#!/bin/bash
# Stop on error.
set -e
CurrentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
currentScriptName=`basename "$0"`

echo "CurrentDir: $CurrentDir"
echo "currentScriptName: $currentScriptName"

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
DeploymentSubscriptionId=$(<bin/subscription-id.txt)
    if [ "$DeploymentSubscriptionId" = "" ]; then
        echo "Please set 'DeploymentSubscriptionId' ..."
        exit 1 # terminate and indicate error
    fi
fi

./AzLogin.sh

./ConnectAKS.sh

./DeployPodIdentity.sh \
--environmentName="$APP_ASPNETCORE_ENVIRONMENT" \
--compactRegion="$compactRegion" \
--DeploymentSubscriptionId="$DeploymentSubscriptionId"

./DeployNginx.sh

./DeployPrometheusOperator.sh \
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

if [ "$NoWait" = "" ]; then
    echo "Wait for extra 120 seconds to make sure the Public IP address is provisioned"
    sleep 120s
fi

./ExecuteDeploymentRunner.sh \
--ProvisionAction="UpdateAKSPublicIpInTrafficManager" \
--EnvName="$APP_ASPNETCORE_ENVIRONMENT" \
--Region="$REGION"

for script in "$CurrentDir"/3_*.sh
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