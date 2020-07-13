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
IMG_mdsd="genevamdsd:master_295"
IMG_mdm="genevamdm:master_41"
IMG_fluentd="genevafluentd_td-agent:master_139"
IMG_azsecpack="genevasecpackinstall:master_53"
IMG_prommdm="shared/prom-mdm-converter:2.0.master.20200708.1"

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

if [ "$TenantId" == "72f988bf-86f1-41af-91ab-2d7cd011db47" ]; then
    echo "Microsoft Tenant"
    LiftrACRResourceId="/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/liftr-acr-rg/providers/Microsoft.ContainerRegistry/registries/liftrmsacr"
else
    echo "AME Tenant"
    LiftrACRResourceId="/subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourceGroups/liftr-acr-rg/providers/Microsoft.ContainerRegistry/registries/LiftrAMEACR"
fi

set -x

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'nginx-ingress' chart images"
echo "import quay.io/kubernetes-ingress-controller/nginx-ingress-controller:0.32.0"
az acr import --name "$ACRName" --source quay.io/kubernetes-ingress-controller/nginx-ingress-controller:0.32.0 --force

echo "import k8s.gcr.io/defaultbackend-amd64:1.5"
az acr import --name "$ACRName" --source k8s.gcr.io/defaultbackend-amd64:1.5 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'kube-state-metrics' chart images"
echo "import quay.io/coreos/kube-state-metrics:v1.9.5"
az acr import --name "$ACRName" --source quay.io/coreos/kube-state-metrics:v1.9.5 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'prometheus-node-exporter' chart images"
echo "import quay.io/prometheus/node-exporter:v0.18.1"
az acr import --name "$ACRName" --source quay.io/prometheus/node-exporter:v0.18.1 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'prometheus-operator' chart images"
echo "import quay.io/prometheus/alertmanager:v0.20.0"
az acr import --name "$ACRName" --source quay.io/prometheus/alertmanager:v0.20.0 --force

echo "import docker.io/squareup/ghostunnel:v1.5.2"
az acr import --name "$ACRName" --source docker.io/squareup/ghostunnel:v1.5.2 --force

echo "import docker.io/jettech/kube-webhook-certgen:v1.0.0"
az acr import --name "$ACRName" --source docker.io/jettech/kube-webhook-certgen:v1.0.0 --force

echo "import quay.io/coreos/prometheus-operator:v0.37.0"
az acr import --name "$ACRName" --source quay.io/coreos/prometheus-operator:v0.37.0 --force

echo "import quay.io/coreos/configmap-reload:v0.0.1"
az acr import --name "$ACRName" --source quay.io/coreos/configmap-reload:v0.0.1 --force

echo "import quay.io/coreos/prometheus-config-reloader:v0.37.0"
az acr import --name "$ACRName" --source quay.io/coreos/prometheus-config-reloader:v0.37.0 --force

echo "import k8s.gcr.io/hyperkube:v1.12.1"
az acr import --name "$ACRName" --source k8s.gcr.io/hyperkube:v1.12.1 --force

echo "import quay.io/prometheus/prometheus:v2.15.2"
az acr import --name "$ACRName" --source quay.io/prometheus/prometheus:v2.15.2 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "Latest geneva image versions: https://genevamondocs.azurewebsites.net/collect/environments/linuxcontainers.html"
echo "import geneva images"
echo "Please make sure the Geneva images are imported to the shared liftr ACR first: https://msazure.visualstudio.com/Liftr/_git/Liftr.Common?path=%2Ftools%2Fdependency-images%2FPrepareGenevaImages.sh"
az acr import --name "$ACRName" --source $IMG_mdsd --registry $LiftrACRResourceId --force
az acr import --name "$ACRName" --source $IMG_mdm --registry $LiftrACRResourceId --force
az acr import --name "$ACRName" --source $IMG_fluentd --registry $LiftrACRResourceId --force
az acr import --name "$ACRName" --source $IMG_azsecpack --registry $LiftrACRResourceId --force
az acr import --name "$ACRName" --source $IMG_prommdm --registry $LiftrACRResourceId --force

set +x
echo "Imported all the dependency images"