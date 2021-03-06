#!/bin/bash
# Stop on error.
set -e
CurrentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
currentScriptName=`basename "$0"`

echo "CurrentDir: $CurrentDir"
echo "currentScriptName: $currentScriptName"

if [ "$GenevaParametersFile" = "" ]; then
    echo "Please set the file path to the Geneva parameters using variable 'GenevaParametersFile' ..."
    exit 1 # terminate and indicate error
fi

echo "GenevaParametersFile: $GenevaParametersFile"

./ExecuteDeploymentRunner.sh \
--ProvisionAction="CreateOrUpdateRegionalCompute" \
--EnvName="$APP_ASPNETCORE_ENVIRONMENT" \
--Region="$REGION"

if [ "$ComputeType" = "" ]; then
ComputeType=$(<bin/compute-type.txt)
fi

if [ "$MDM_IMAGE_VERSION" = "" ]; then
MDM_IMAGE_VERSION="latest"
fi

if [ "$MDSD_IMAGE_VERSION" = "" ]; then
MDSD_IMAGE_VERSION="latest"
fi

if [ "$FLUENTD_IMAGE_VERSION" = "" ]; then
FLUENTD_IMAGE_VERSION="latest"
fi

if [ "$AZSECPACK_IMAGE_VERSION" = "" ]; then
AZSECPACK_IMAGE_VERSION="latest"
fi

if [ "$PROMMDMCONVERTER_IMAGE_VERSION" = "" ]; then
PROMMDMCONVERTER_IMAGE_VERSION="latest"
fi

if [ "$ComputeType" = "vmss" ]; then
  echo "Successfully finished running: $currentScriptName"
  echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"
  exit 0
fi

if [ "$DeploymentSubscriptionId" = "" ]; then
DeploymentSubscriptionId=$(<bin/subscription-id.txt)
    if [ "$DeploymentSubscriptionId" = "" ]; then
        echo "Please set 'DeploymentSubscriptionId' ..."
        exit 1 # terminate and indicate error
    fi
fi

./AzLogin.sh

./ConnectAKS.sh

./DeployPrometheusStack.sh \
--environmentName="$APP_ASPNETCORE_ENVIRONMENT" \
--compactRegion="$REGION" \
--DeploymentSubscriptionId="$DeploymentSubscriptionId"

./DeployPodIdentity.sh \
--environmentName="$APP_ASPNETCORE_ENVIRONMENT" \
--compactRegion="$REGION" \
--DeploymentSubscriptionId="$DeploymentSubscriptionId"

./DeployKeyVaultCSI.sh

./DeployNginx.sh

./DeployProm2IcM.sh

./DeployThanosIngress.sh \
--environmentName="$APP_ASPNETCORE_ENVIRONMENT" \
--compactRegion="$REGION" \
--DeploymentSubscriptionId="$DeploymentSubscriptionId"

./DeployGenevaMonitoring.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId" \
--GenevaParametersFile="$GenevaParametersFile" \
--environmentName="$APP_ASPNETCORE_ENVIRONMENT" \
--compactRegion="$REGION" \
--Region="$REGION" \
--gcs_region="$REGION" \
--mdmVersion="$MDM_IMAGE_VERSION" \
--mdsdVersion="$MDSD_IMAGE_VERSION" \
--fluentdVersion="$FLUENTD_IMAGE_VERSION" \
--secpackVersion="$AZSECPACK_IMAGE_VERSION" \
--prommdmconverterVersion="$PROMMDMCONVERTER_IMAGE_VERSION"

for script in "$CurrentDir"/3_*.sh
do
  if [[ "$script" != *"$currentScriptName"* ]]; then
    echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
    echo "Executing extension script '$script' :"
    $script
    echo "Finished Executing extension script '$script'."
    echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
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