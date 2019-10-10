#!/bin/bash

# Stop on error.
set -e

for i in "$@"
do
case $i in
    --ProvisionAction=*)
    ProvisionAction="${i#*=}"
    shift # past argument=value
    ;;
    --DeploymentSubscriptionId=*)
    DeploymentSubscriptionId="${i#*=}"
    shift # past argument=value
    ;;
    --ConfigFilePath=*)
    ConfigFilePath="${i#*=}"
    shift # past argument=value
    ;;
    --AKSSvcLabel=*)
    AKSSvcLabel="${i#*=}"
    shift # past argument=value
    ;;
    *)
    echo "Not matched option '${i#*=}' passed in."
    exit 1
    ;;
esac
done

if [ -z ${DeploymentSubscriptionId+x} ]; then
    echo "DeploymentSubscriptionId is blank."
    exit 1
fi

if [ -z ${ProvisionAction+x} ]; then
    echo "ProvisionAction is blank."
    exit 1
fi

if [ -z ${ConfigFilePath+x} ]; then
    echo "ConfigFilePath is blank."
    exit 1
fi

if [ "$ProvisionAction" = "UpdateAKSPublicIpInTrafficManager" ]; then
    if [ -z ${AKSSvcLabel+x} ]; then
        echo "AKSSvcLabel is blank."
        exit 1
    fi
fi

echo "Start deployment against subscription Id: $DeploymentSubscriptionId"
echo "Config file path: $ConfigFilePath"

cd bin
echo "----------------------------------------------------------------------------------------------"
echo "Configration file content: "
cat $ConfigFilePath
echo
echo "----------------------------------------------------------------------------------------------"

if [ "$AKSSvcLabel" = "" ]; then
    dotnet Deployment.Runner.dll \
    -a "$ProvisionAction" \
    -f "$ConfigFilePath" \
    -s "$DeploymentSubscriptionId"
else
    dotnet Deployment.Runner.dll \
    -a "$ProvisionAction" \
    -f "$ConfigFilePath" \
    -s "$DeploymentSubscriptionId" \
    -l "$AKSSvcLabel"
fi

exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Failed to run provisioning runner."
    exit $exit_code
fi

echo "----------------------------------------------------------------------------------------------"
echo "Finished running the provisioning runner."
echo "----------------------------------------------------------------------------------------------"