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

checkAndRegisterProvider "Microsoft.VirtualMachineImages"
checkAndRegisterProvider "Microsoft.Storage"
checkAndRegisterProvider "Microsoft.Compute"
checkAndRegisterProvider "Microsoft.KeyVault"
checkAndRegisterProvider "Microsoft.ManagedIdentity"

# register and enable for shared image gallery
echo "az feature register --namespace Microsoft.VirtualMachineImages --name VirtualMachineTemplatePreview"
az feature register --namespace Microsoft.VirtualMachineImages --name VirtualMachineTemplatePreview > azFeatureVirtualMachineTemplatePreview.txt

echo "az feature register --namespace Microsoft.Compute --name GalleryPreview"
az feature register --namespace Microsoft.Compute --name GalleryPreview > azFeatureGalleryPreview.txt

echo "-----------------------------------------------------------------"
echo "Finished registering Azure Image builder features and providers."
echo "-----------------------------------------------------------------"