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

echo "az aks get-credentials -g $AKSRGName -n $AKSName"
az aks get-credentials -g "$AKSRGName" -n "$AKSName"

$Helm repo add stable https://kubernetes-charts.storage.googleapis.com/

# Deploy identity infrastructure daemonset to default namespace
echo "helm upgrade aks-pod-identity-infra"
$Helm upgrade aks-pod-identity-infra --install --recreate-pods --namespace default aad-pod-identity-infra-*.tgz

echo "Start checking aks-pod-identity-infra deployment status"
sleep_interval=10
time_out=30
wait_counter=0
# we will query for the api version of the custom resources to be available
# default timeout 10 * 30 = 300 sec = 5 min
while true
do
    echo "Waiting for the AKS pod identity infra deployment " && sleep ${sleep_interval} && ((wait_counter++))

    kubectl get pods | grep "^nmi-\|^mic" > status.txt
    podCount=$(cat status.txt | wc -l)
    runningPodCount=$(cat status.txt | grep Running | wc -l)

    if [[ ($podCount -gt 1) && ($podCount -eq $runningPodCount) ]]; then
        echo "Finished AAD Pod Identity Infra deployment. All pods are running"
        break;
    fi

    if [[ $wait_counter -eq $time_out ]]; then
        echo "Timeout waiting for the AKS pod identity infra deployment."
        break;
    fi
done

# Deploy azure identity binding components to default namespace
echo "helm upgrade aks-pod-identity-binding"
$Helm upgrade aks-pod-identity-binding --install --recreate-pods \
--values "aad-pod-identity.values.yaml" \
--set azureIdentity.resourceID=$MSIResourceId \
--set azureIdentity.clientID=$MSIClientId \
--namespace default aad-pod-identity-binding-*.tgz

echo "-------------------------------------"
echo "Finished helm upgrade aks pod identity chart"
echo "-------------------------------------"