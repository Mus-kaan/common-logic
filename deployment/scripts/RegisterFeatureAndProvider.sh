#!/bin/bash

# Stop on error.
set -e

checkAndRegisterProvider()
{
    providerName=$1
    echo "Start checking provider registration of '$providerName'"
    registrationState=$(az provider show -n $providerName | grep registrationState)
    if [[ $registrationState == *"Registered"* ]]; then
    echo "$providerName is Registered"
    else
        echo "$providerName $registrationState"
        az provider register -n "$providerName"
    fi
}

for i in "$@"
do
case $i in
    --DeploymentSubscriptionId=*)
    DeploymentSubscriptionId="${i#*=}"
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

echo "az login --identity"
az login --identity
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "az login failed."
    exit $exit_code
fi

echo "az account set -s $DeploymentSubscriptionId"
az account set -s "$DeploymentSubscriptionId"
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "az account set failed."
    exit $exit_code
fi

az feature register --namespace Microsoft.VirtualMachineImages --name VirtualMachineTemplatePreview
az feature show --namespace Microsoft.VirtualMachineImages --name VirtualMachineTemplatePreview | grep state

checkAndRegisterProvider "Microsoft.Storage"
checkAndRegisterProvider "Microsoft.Compute"
checkAndRegisterProvider "Microsoft.VirtualMachineImages"

# register and enable for shared image gallery
az feature register --namespace Microsoft.Compute --name GalleryPreview

echo "-----------------------------------------------------------------"
echo "Finished registering Azure Image builder features and providers."
echo "-----------------------------------------------------------------"