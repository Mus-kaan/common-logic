#!/bin/bash

# Stop on error.
set -e

if [ "$NoWait" = "true" ]; then
    echo "Skip import images when 'NoWait' is set"
    exit 0
fi

# [[[GENEVA_UPDATE_CHANGE_HERE]]]
# The version are referenced at three places. You need to update all of them. Please search for this sentence.
echo "Latest geneva image versions: https://genevamondocs.azurewebsites.net/collect/references/linuxcontainers.html"
IMG_mdsd="genevamdsd:master_337"
IMG_mdm="genevamdm:master_52"
IMG_fluentd="genevafluentd_td-agent:master_157"
IMG_azsecpack="genevasecpackinstall:master_64"
IMG_prommdm="shared/prom-mdm-converter:2.0.master.20200921.1"

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

if [ "$ComputeType" = "" ]; then
ComputeType=$(<bin/compute-type.txt)
fi

set -x

if [ "$ComputeType" = "aks" ]; then
echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'ingress-nginx' chart images"
echo "import k8s.gcr.io/ingress-nginx/controller:v0.41.0"
az acr import --name "$ACRName" --source k8s.gcr.io/ingress-nginx/controller:v0.41.0 --force

echo "import k8s.gcr.io/defaultbackend-amd64:1.5"
az acr import --name "$ACRName" --source k8s.gcr.io/defaultbackend-amd64:1.5 --force

echo "import docker.io/jettech/kube-webhook-certgen:v1.5.0"
az acr import --name "$ACRName" --source docker.io/jettech/kube-webhook-certgen:v1.5.0 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'kube-state-metrics' chart images"
echo "import quay.io/coreos/kube-state-metrics:v1.9.7"
az acr import --name "$ACRName" --source quay.io/coreos/kube-state-metrics:v1.9.7 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'prometheus-node-exporter' chart images"
echo "import quay.io/prometheus/node-exporter:v1.0.1"
az acr import --name "$ACRName" --source quay.io/prometheus/node-exporter:v1.0.1 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'kube-prometheus-stack' chart images"
echo "import quay.io/prometheus/alertmanager:v0.21.0"
az acr import --name "$ACRName" --source quay.io/prometheus/alertmanager:v0.21.0 --force

echo "import quay.io/prometheus-operator/prometheus-operator:v0.43.1"
az acr import --name "$ACRName" --source quay.io/prometheus-operator/prometheus-operator:v0.43.1 --force

echo "import quay.io/thanos/thanos:v0.15.0"
az acr import --name "$ACRName" --source quay.io/thanos/thanos:v0.15.0 --force

echo "import docker.io/jimmidyson/configmap-reload:v0.4.0"
az acr import --name "$ACRName" --source docker.io/jimmidyson/configmap-reload:v0.4.0 --force

echo "import quay.io/prometheus-operator/prometheus-config-reloader:v0.43.1"
az acr import --name "$ACRName" --source quay.io/prometheus-operator/prometheus-config-reloader:v0.43.1 --force

echo "import quay.io/prometheus/prometheus:v2.22.1"
az acr import --name "$ACRName" --source quay.io/prometheus/prometheus:v2.22.1 --force
fi

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "Latest geneva image versions: https://genevamondocs.azurewebsites.net/collect/environments/linuxcontainers.html"
echo "import geneva images"

if [ "$TenantId" == "72f988bf-86f1-41af-91ab-2d7cd011db47" ]; then
    echo "Using Liftr Microsoft Tenant ACR. Please make sure the EV2 MI has 'Reader' role over the ms tenant ACR 'liftrmsacr'."
    LiftrACRResourceId="/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/liftr-acr-rg/providers/Microsoft.ContainerRegistry/registries/liftrmsacr"
else
    echo "Using Liftr AME Tenant ACR. Please make sure the EV2 MI has 'Reader' role over the ame tenant ACR 'liftrameacr'."
    LiftrACRResourceId="/subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourceGroups/liftr-acr-rg/providers/Microsoft.ContainerRegistry/registries/LiftrAMEACR"
fi

az acr import --name "$ACRName" --source $IMG_mdsd --registry $LiftrACRResourceId --force
az acr import --name "$ACRName" --source $IMG_mdm --registry $LiftrACRResourceId --force
az acr import --name "$ACRName" --source $IMG_fluentd --registry $LiftrACRResourceId --force
az acr import --name "$ACRName" --source $IMG_azsecpack --registry $LiftrACRResourceId --force
az acr import --name "$ACRName" --source $IMG_prommdm --registry $LiftrACRResourceId --force

set +x
echo "Imported all the dependency images"