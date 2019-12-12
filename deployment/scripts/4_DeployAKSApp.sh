#!/bin/bash
# Stop on error.
set -e
currentScriptName=`basename "$0"`

if [ "$APP_ASPNETCORE_ENVIRONMENT" = "" ]; then
APP_ASPNETCORE_ENVIRONMENT="Production"
fi

AKSSvcLabel="nginx-ingress-controller"
AKSAppChartPackage="liftr-*.tgz"

echo "Get Key Vault endpoint and save on disk."
./ExecuteDeploymentRunner.sh \
--ProvisionAction="GetKeyVaultEndpoint" \
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

./ImportCDPxImages.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId"

./DeployAKSApp.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId" \
--APP_ASPNETCORE_ENVIRONMENT="$APP_ASPNETCORE_ENVIRONMENT" \
--AKSAppChartPackage="$AKSAppChartPackage"

./ExecuteDeploymentRunner.sh \
--ProvisionAction="UpdateAKSPublicIpInTrafficManager" \
--EnvName="$APP_ASPNETCORE_ENVIRONMENT" \
--Region="$REGION" \
--AKSSvcLabel="$AKSSvcLabel"

echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"