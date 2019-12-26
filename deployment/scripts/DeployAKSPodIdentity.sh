#!/bin/bash
# Stop on error.
set -e

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

Helm="./helm"

echo "************************************************************"
echo "Start helm upgrade pod identity chart ..."
echo "************************************************************"

echo "DeploymentSubscriptionId: $DeploymentSubscriptionId"

if [ "$AKSRGName" = "" ]; then
echo "Read AKSRGName from file 'bin/aks-rg.txt'."
AKSRGName=$(<bin/aks-rg.txt)
    if [ "$AKSRGName" = "" ]; then
        echo "Please set the name of the AKS cluster Resource Group using variable 'AKSRGName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "AKSRGName: $AKSRGName"

if [ "$AKSName" = "" ]; then
echo "Read AKSName from file 'bin/aks-name.txt'."
AKSName=$(<bin/aks-name.txt)
    if [ "$AKSName" = "" ]; then
        echo "Please set the name of the AKS cluster using variable 'AKSName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "AKSName: $AKSName"

if [ "$MSIResourceId" = "" ]; then
echo "Read MSIResourceId from file 'bin/msi-resourceId.txt'."
MSIResourceId=$(<bin/msi-resourceId.txt)
    if [ "$MSIResourceId" = "" ]; then
        echo "Please set the MSI resource Id using variable 'MSIResourceId' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "MSIResourceId: $MSIResourceId"

if [ "$MSIClientId" = "" ]; then
echo "Read MSIClientId from file 'bin/msi-clientId.txt'."
MSIClientId=$(<bin/msi-clientId.txt)
    if [ "$MSIClientId" = "" ]; then
        echo "Please set the MSI client Id using variable 'MSIClientId' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "MSIClientId: $MSIClientId"

echo "az aks get-credentials -g $AKSRGName -n $AKSName"
az aks get-credentials -g "$AKSRGName" -n "$AKSName"

# Deploy identity infrastructure daemonset to default namespace
echo "helm upgrade aks-pod-identity-infra"
$Helm upgrade aks-pod-identity-infra --install --namespace default aad-pod-identity-infra-*.tgz

echo "Start waiting for aks-pod-identity-infra deployment to finish ..."
kubectl rollout status daemonset/nmi
kubectl rollout status deployment/mic

# Deploy azure identity binding components to default namespace
echo "helm upgrade aks-pod-identity-binding"
echo "If this part failed with 'cannot find aad pod api, please retry the release.'"
$Helm upgrade aks-pod-identity-binding --install \
--values "aad-pod-identity.values.yaml" \
--set azureIdentity.resourceID=$MSIResourceId \
--set azureIdentity.clientID=$MSIClientId \
--namespace default aad-pod-identity-binding-*.tgz

echo "-------------------------------------"
echo "Finished helm upgrade aks pod identity chart"
echo "-------------------------------------"