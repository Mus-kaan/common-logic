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

./RunProvisioningRunner.sh \
--ProvisionAction="CreateOrUpdateRegionalData" \
--DeploymentSubscriptionId="$DeploymentSubscriptionId" \
--ConfigFilePath="$ConfigFilePath"