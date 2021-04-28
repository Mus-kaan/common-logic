#!/bin/bash
# Stop on error.
set -e
namespace="aad-pod-id"

for i in "$@"
do
case $i in
    --DeploymentSubscriptionId=*)
    DeploymentSubscriptionId="${i#*=}"
    shift # past argument=value
    ;;
    --compactRegion=*)
    compactRegion="${i#*=}"
    shift # past argument=value
    ;;
    --environmentName=*)
    environmentName="${i#*=}"
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
set -x

sed -i "s|MSI_RESOURCE_ID_PLACEHOLDER|$MSIResourceId|g" pod-id-values.yaml
sed -i "s|MSI_CLIENT_ID_PLACEHOLDER|$MSIClientId|g" pod-id-values.yaml

# TODO: Add OPA. details: https://azure.github.io/aad-pod-identity/docs/configure/aad_pod_identity_on_kubenet/
$Helm upgrade aad-pod-id-rel aad-pod-identity-*.tgz --install --create-namespace \
--wait --force \
--namespace $namespace \
-f pod-id-values.yaml \
--set nmi.allowNetworkPluginKubenet=true

set +x
echo "-------------------------------------"
echo "Finished helm upgrade aks pod identity chart"
echo "-------------------------------------"