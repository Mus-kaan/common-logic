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
    *)
    echo "Not matched option '${i#*=}' passed in."
    exit 1
    ;;
esac
done

if [ "$DeploymentSubscriptionId" = "" ]; then
echo "Read DeploymentSubscriptionId from file 'bin/subscription-id.txt'."
DeploymentSubscriptionId=$(<bin/subscription-id.txt)
    if [ "$DeploymentSubscriptionId" = "" ]; then
        echo "Please set 'DeploymentSubscriptionId' ..."
        exit 1 # terminate and indicate error
    fi
fi

if [ "$ACRName" = "" ]; then
echo "Read ACRName from file 'bin/acr-name.txt'."
ACRName=$(<bin/acr-name.txt)
    if [ "$ACRName" = "" ]; then
        echo "Please set 'ACRName' ..."
        exit 1 # terminate and indicate error
    fi
fi

echo "import prometheus images"

echo "import docker.io/library/busybox:latest"
az acr import --name "$ACRName" --source docker.io/library/busybox:latest --force

echo "import docker.io/jimmidyson/configmap-reload:v0.2.2"
az acr import --name "$ACRName" --source docker.io/jimmidyson/configmap-reload:v0.2.2 --force

echo "import docker.io/prom/prometheus:v2.4.3"
az acr import --name "$ACRName" --source docker.io/prom/prometheus:v2.4.3 --force

echo "import docker.io/prom/node-exporter:v0.16.0"
az acr import --name "$ACRName" --source docker.io/prom/node-exporter:v0.16.0 --force

echo "import quay.io/coreos/kube-state-metrics:v1.4.0"
az acr import --name "$ACRName" --source quay.io/coreos/kube-state-metrics:v1.4.0 --force

echo "import k8s.gcr.io/defaultbackend-amd64:1.5"
az acr import --name "$ACRName" --source k8s.gcr.io/defaultbackend-amd64:1.5 --force

echo "import docker.io/jettech/kube-webhook-certgen:v1.0.0"
az acr import --name "$ACRName" --source docker.io/jettech/kube-webhook-certgen:v1.0.0 --force

echo "import quay.io/kubernetes-ingress-controller/nginx-ingress-controller"
az acr import --name "$ACRName" --source quay.io/kubernetes-ingress-controller/nginx-ingress-controller:0.26.1 --force

echo "Latest geneva image versions: https://genevamondocs.azurewebsites.net/collect/environments/linuxcontainers.html"
echo "import geneva images"
az acr import --name "$ACRName" --source genevamdsd:master_246 --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
az acr import --name "$ACRName" --source genevamdm:master_28 --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
az acr import --name "$ACRName" --source genevafluentd_td-agent:master_124 --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
az acr import --name "$ACRName" --source genevasecpackinstall:master_30 --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force

echo "import liftrcr.azurecr.io/prom-mdm-converter:latest"
az acr import --name "$ACRName" --source prom-mdm-converter:latest --registry /subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourceGroups/liftr-images-wus-rg/providers/Microsoft.ContainerRegistry/registries/liftrcr --force

echo "ACRName: $ACRName"