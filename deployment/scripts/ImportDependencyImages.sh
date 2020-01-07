#!/bin/bash

# Stop on error.
set -e

# The version are referenced at three places. You need to update all of them. Please search for this sentence.
echo "Latest geneva image versions: https://genevamondocs.azurewebsites.net/collect/environments/linuxcontainers.html"
IMG_mdsd="genevamdsd:master_246"
IMG_mdm="genevamdm:master_28"
IMG_fluentd="genevafluentd_td-agent:master_124"
IMG_azsecpack="genevasecpackinstall:master_30"
IMG_prommdm="shared/prom-mdm-converter:2.0.master.20200106.5"

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

if [ "$TenantId" = "" ]; then
echo "Read TenantId from file 'bin/tenant-id.txt'."
TenantId=$(<bin/tenant-id.txt)
    if [ "$TenantId" = "" ]; then
        echo "Please set 'TenantId' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "TenantId: $TenantId"

if [ "$TenantId" == "72f988bf-86f1-41af-91ab-2d7cd011db47" ]; then
    echo "Microsoft Tenant"
    LiftrACRResourceId="/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/liftr-acr-rg/providers/Microsoft.ContainerRegistry/registries/liftrmsacr"
else
    echo "AME Tenant"
    LiftrACRResourceId="/subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourceGroups/liftr-acr-rg/providers/Microsoft.ContainerRegistry/registries/LiftrAMEACR"
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
az acr import --name "$ACRName" --source $IMG_mdsd --registry $LiftrACRResourceId --force
az acr import --name "$ACRName" --source $IMG_mdm --registry $LiftrACRResourceId --force
az acr import --name "$ACRName" --source $IMG_fluentd --registry $LiftrACRResourceId --force
az acr import --name "$ACRName" --source $IMG_azsecpack --registry $LiftrACRResourceId --force

echo "import prom-mdm-converter"
az acr import --name "$ACRName" --source $IMG_prommdm --registry $LiftrACRResourceId --force

echo "Imported all the dependency images"