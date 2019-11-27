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

if [ "$gcs_region" = "" ]; then
    echo "Please set GCS region using variable 'gcs_region' ..."
    exit 1 # terminate and indicate error
fi

if [ "$GenevaParametersFile" = "" ]; then
    echo "Please set the file path to the Geneva parameters using variable 'GenevaParametersFile' ..."
    exit 1 # terminate and indicate error
fi

echo "GenevaParametersFile: $GenevaParametersFile"

./RunProvisioningRunner.sh \
--ProvisionAction="CreateOrUpdateRegionalCompute" \
--DeploymentSubscriptionId="$DeploymentSubscriptionId" \
--ConfigFilePath="$ConfigFilePath"

./DeployAKSPodIdentity.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId"

./DeployGenevaMonitoring.sh \
--DeploymentSubscriptionId="$DeploymentSubscriptionId" \
--GenevaParametersFile="$GenevaParametersFile" \
--gcs_region="$gcs_region"