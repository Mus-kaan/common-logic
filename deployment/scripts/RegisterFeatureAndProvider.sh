#!/bin/bash

# Stop on error.
set -e

checkAndRegisterProvider()
{
    providerName=$1
    echo "Start checking provider registration of '$providerName'"
    registrationState=$(az provider show -n $providerName | grep registrationState)
    echo "$providerName: $registrationState"
    if [[ $registrationState == *"NotRegistered"* ]]; then
        echo "Registering provider: $providerName"
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

checkAndRegisterProvider "Microsoft.Storage"
checkAndRegisterProvider "Microsoft.Compute"
checkAndRegisterProvider "Microsoft.VirtualMachineImages"
checkAndRegisterProvider "Microsoft.Insights"
checkAndRegisterProvider "Microsoft.OperationalInsights"
checkAndRegisterProvider "Microsoft.OperationsManagement"
checkAndRegisterProvider "Microsoft.DocumentDB"
checkAndRegisterProvider "Microsoft.KeyVault"
checkAndRegisterProvider "Microsoft.ManagedIdentity"
checkAndRegisterProvider "Microsoft.DomainRegistration"
checkAndRegisterProvider "Microsoft.Network"
checkAndRegisterProvider "Microsoft.EventHub"
checkAndRegisterProvider "microsoft.insights"
checkAndRegisterProvider "Microsoft.ContainerService"
checkAndRegisterProvider "Microsoft.ContainerRegistry"

# register and enable for shared image gallery
echo "az feature register --namespace Microsoft.VirtualMachineImages --name VirtualMachineTemplatePreview"
az feature register --namespace Microsoft.VirtualMachineImages --name VirtualMachineTemplatePreview

echo "az feature register --namespace Microsoft.Compute --name GalleryPreview"
az feature register --namespace Microsoft.Compute --name GalleryPreview

echo "-----------------------------------------------------------------"
echo "Finished registering Azure features and providers."
echo "-----------------------------------------------------------------"