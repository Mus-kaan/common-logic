#!/bin/bash

# Stop on error.
set -e

for i in "$@"
do
case $i in
    --ACRName=*)
    ACRName="${i#*=}"
    shift # past argument=value
    ;;
    --DeploymentSubscriptionId=*)
    DeploymentSubscriptionId="${i#*=}"
    shift # past argument=value
    ;;
    --ImageMetadataPath=*)
    ImageMetadataPath="${i#*=}"
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

if [ "$ACRName" = "" ]; then
echo "Read ACRName from file 'bin/acr-name.txt'."
ACRName=$(<bin/acr-name.txt)
    if [ "$ACRName" = "" ]; then
        echo "Please set 'ACRName' ..."
        exit 1 # terminate and indicate error
    fi
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

set +e

echo "import prometheus images"

echo "import docker.io/library/busybox:latest"
az acr import --name "$ACRName" --source docker.io/library/busybox:latest

echo "import docker.io/jimmidyson/configmap-reload:v0.2.2"
az acr import --name "$ACRName" --source docker.io/jimmidyson/configmap-reload:v0.2.2

echo "import docker.io/prom/prometheus:v2.4.3"
az acr import --name "$ACRName" --source docker.io/prom/prometheus:v2.4.3

echo "import docker.io/prom/node-exporter:v0.16.0"
az acr import --name "$ACRName" --source docker.io/prom/node-exporter:v0.16.0

echo "import quay.io/coreos/kube-state-metrics:v1.4.0"
az acr import --name "$ACRName" --source quay.io/coreos/kube-state-metrics:v1.4.0


echo "import liftrcr.azurecr.io/prom-mdm-converter:latest"
az acr import --name "$ACRName" --source prom-mdm-converter:latest --registry /subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourceGroups/liftr-images-wus-rg/providers/Microsoft.ContainerRegistry/registries/liftrcr

echo "import k8s.gcr.io/defaultbackend-amd64:1.5"
az acr import --name "$ACRName" --source k8s.gcr.io/defaultbackend-amd64:1.5

echo "import docker.io/jettech/kube-webhook-certgen:v1.0.0"
az acr import --name "$ACRName" --source docker.io/jettech/kube-webhook-certgen:v1.0.0

echo "import quay.io/kubernetes-ingress-controller/nginx-ingress-controller"
az acr import --name "$ACRName" --source quay.io/kubernetes-ingress-controller/nginx-ingress-controller:0.26.1

echo "import geneva images"
az acr import --name "$ACRName" --source genevamdsd:master_236 --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva
az acr import --name "$ACRName" --source genevafluentd_td-agent:master_110 --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva
az acr import --name "$ACRName" --source genevamdm:master_14 --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva
az acr import --name "$ACRName" --source genevasecpackinstall:master_17 --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva

echo "ACRName: $ACRName"

exit 0