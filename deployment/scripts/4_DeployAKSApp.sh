#!/bin/bash

# Stop on error.
set -e


if [ "$DeploymentSubscriptionId" = "" ]; then
    echo "Please set the deployment subscription Id using variable 'DeploymentSubscriptionId' ..."
    exit 1 # terminate and indicate error
fi

if [ "$ConfigFilePath" = "" ]; then
    echo "Please set a path to the config file using variable 'ConfigFilePath' ..."
    exit 1 # terminate and indicate error
fi

if [ "$APP_ASPNETCORE_ENVIRONMENT" = "" ]; then
APP_ASPNETCORE_ENVIRONMENT="Development"
fi

AKSSvcLabel="nginx-ingress-controller"
AKSAppChartPackage="liftr-rp-web-*.tgz"

echo "Get Key Vault endpoint and save on disk."
./RunProvisioningRunner.sh \
--ProvisionAction="GetComputeKeyVaultEndpoint" \
--DeploymentSubscriptionId="$DeploymentSubscriptionId" \
--ConfigFilePath="$ConfigFilePath"

./DeployAKSApp.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId" \
--AKSAppChartPackage="$AKSAppChartPackage"

./RunProvisioningRunner.sh \
--ProvisionAction="UpdateAKSPublicIpInTrafficManager" \
--DeploymentSubscriptionId="$DeploymentSubscriptionId" \
--ConfigFilePath="$ConfigFilePath" \
--APP_ASPNETCORE_ENVIRONMENT="$APP_ASPNETCORE_ENVIRONMENT" \
--AKSSvcLabel="$AKSSvcLabel"
echo "-----------------------------------------------------------------"
echo "Finished update AKS service public IP in traffic manager"
echo "-----------------------------------------------------------------"