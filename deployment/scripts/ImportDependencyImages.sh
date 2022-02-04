#!/bin/bash

# Stop on error.
set -e

if [ "$NoWait" = "true" ]; then
    echo "Skip import images when 'NoWait' is set"
    exit 0
fi

# [[[GENEVA_UPDATE_CHANGE_HERE]]]
# The version are referenced at two places. You need to update all of them. Please search for this sentence.
# latest prom mdm image version: https://msazure.visualstudio.com/Liftr/_build?definitionId=113171&_a=summary&view=runs
echo "Latest geneva image versions: https://genevamondocs.azurewebsites.net/collect/references/linuxcontainers.html"
IMG_mdsd="genevamdsd:master_20220202.1"
IMG_mdm="genevamdm:master_20220202.1"
IMG_fluentd="genevafluentd_td-agent:master_20220202.1"
IMG_azsecpack="genevasecpackinstall:master_20220202.1"
IMG_prommdm="shared/prom-mdm-converter:2.0.master.20220204.1"

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

if [ "$ComputeType" = "" ]; then
ComputeType=$(<bin/compute-type.txt)
fi

set -x

if [ "$ComputeType" = "aks" ]; then
echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'keda' chart images"
echo "import ghcr.io/kedacore/keda:2.5.0"
az acr import --name "$ACRName" --source ghcr.io/kedacore/keda:2.5.0 --force

echo "import ghcr.io/kedacore/keda-metrics-apiserver:2.5.0"
az acr import --name "$ACRName" --source ghcr.io/kedacore/keda-metrics-apiserver:2.5.0 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'ingress-nginx' chart images"
echo "import k8s.gcr.io/ingress-nginx/controller:v1.0.1"
az acr import --name "$ACRName" --source k8s.gcr.io/ingress-nginx/controller:v1.0.1 --force

echo "import k8s.gcr.io/defaultbackend-amd64:1.5"
az acr import --name "$ACRName" --source k8s.gcr.io/defaultbackend-amd64:1.5 --force

echo "import k8s.gcr.io/ingress-nginx/kube-webhook-certgen:v1.0"
az acr import --name "$ACRName" --source k8s.gcr.io/ingress-nginx/kube-webhook-certgen:v1.0 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'kube-state-metrics' chart images"
echo "import k8s.gcr.io/kube-state-metrics/kube-state-metrics:v2.3.0"
az acr import --name "$ACRName" --source k8s.gcr.io/kube-state-metrics/kube-state-metrics:v2.3.0 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'prometheus-node-exporter' chart images"
echo "import quay.io/prometheus/node-exporter:v1.3.1"
az acr import --name "$ACRName" --source quay.io/prometheus/node-exporter:v1.3.1 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'grafana' chart images"

echo "import docker.io/grafana/grafana:8.3.3"
az acr import --name "$ACRName" --source docker.io/grafana/grafana:8.3.3 --force

echo "import quay.io/kiwigrid/k8s-sidecar:1.14.2"
az acr import --name "$ACRName" --source quay.io/kiwigrid/k8s-sidecar:1.14.2 --force

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "import 'kube-prometheus-stack' chart images"
echo "import quay.io/prometheus/alertmanager:v0.23.0"
az acr import --name "$ACRName" --source quay.io/prometheus/alertmanager:v0.23.0 --force

echo "import quay.io/prometheus-operator/prometheus-operator:v0.53.1"
az acr import --name "$ACRName" --source quay.io/prometheus-operator/prometheus-operator:v0.53.1 --force

echo "import quay.io/thanos/thanos:v0.23.1"
az acr import --name "$ACRName" --source quay.io/thanos/thanos:v0.23.1 --force

echo "import docker.io/jimmidyson/configmap-reload:v0.4.0"
az acr import --name "$ACRName" --source docker.io/jimmidyson/configmap-reload:v0.4.0 --force

echo "import quay.io/prometheus-operator/prometheus-config-reloader:v0.53.1"
az acr import --name "$ACRName" --source quay.io/prometheus-operator/prometheus-config-reloader:v0.53.1 --force

echo "import quay.io/prometheus/prometheus:v2.32.1"
az acr import --name "$ACRName" --source quay.io/prometheus/prometheus:v2.32.1 --force
fi

echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
echo "Latest geneva image versions: https://genevamondocs.azurewebsites.net/collect/environments/linuxcontainers.html"
echo "import geneva images"

az acr import --name "$ACRName" --source linuxgeneva-microsoft.azurecr.io/$IMG_mdsd --force
az acr import --name "$ACRName" --source linuxgeneva-microsoft.azurecr.io/$IMG_mdm --force
az acr import --name "$ACRName" --source linuxgeneva-microsoft.azurecr.io/$IMG_fluentd --force
az acr import --name "$ACRName" --source linuxgeneva-microsoft.azurecr.io/$IMG_azsecpack --force

az acr import --name "$ACRName" --source liftrmsacr.azurecr.io/$IMG_prommdm --force

set +x
echo "Imported all the dependency images"