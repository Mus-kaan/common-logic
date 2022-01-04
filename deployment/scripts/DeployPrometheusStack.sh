#!/bin/bash
# Stop on error.
set -e

Helm="./helm"
helmReleaseName="prom-rel"
namespace="prometheus"

for i in "$@"
do
case $i in
    --DeploymentSubscriptionId=*)
    DeploymentSubscriptionId="${i#*=}"
    shift # past argument=value
    ;; # past argument=value
    --environmentName=*)
    environmentName="${i#*=}"
    shift # past argument=value
    ;;
    --compactRegion=*)
    compactRegion="${i#*=}"
    shift # past argument=value
    ;;
    *)
    echo "Not matched option '${i#*=}' passed in."
    exit 1
    ;;
esac
done

if [ "$PartnerName" = "" ]; then
echo "Read PartnerName from file 'bin/partner-name.txt'."
PartnerName=$(<bin/partner-name.txt)
    if [ "$PartnerName" = "" ]; then
        echo "Please set the name of the partner using variable 'PartnerName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "PartnerName: $PartnerName"

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

if [ "$liftrACRURI" = "" ]; then
liftrACRURI=$(<bin/acr-endpoint.txt)
    if [ "$liftrACRURI" = "" ]; then
        echo "Please set 'liftrACRURI' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "liftrACRURI: $liftrACRURI"

if [ "$AKSDomain" = "" ]; then
echo "Read AKSDomain from file 'bin/aks-domain.txt'."
AKSDomain=$(<bin/aks-domain.txt)
    if [ "$AKSDomain" = "" ]; then
        echo "Please set aks host name using variable 'AKSDomain' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "AKSDomain: $AKSDomain"
sed -i "s|PLACE_HOLDER_AKS_DOMAIN|$AKSDomain|g" thanos-sidecar-ingress.yaml


set +e
echo "kubectl create namespace $namespace"
kubectl create namespace "$namespace"
set -e

ThanosFlag="--set prometheus.prometheusSpec.replicas=1 "

if [ -f bin/thanos-client-ip.txt ]; then
echo "Thanos is enabled."

if [ "$DiagStorName" = "" ]; then
echo "Read DiagStorName from file 'bin/diag-stor-name.txt'."
DiagStorName=$(<bin/diag-stor-name.txt)
    if [ "$DiagStorName" = "" ]; then
        echo "Please set the name of the diagnostics storage account using variable 'DiagStorName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "DiagStorName: $DiagStorName"

if [ "$DiagStorKey" = "" ]; then
echo "Read DiagStorKey from file 'bin/diag-stor-key.txt'."
DiagStorKey=$(<bin/diag-stor-key.txt)
    if [ "$DiagStorKey" = "" ]; then
        echo "Please set the key of the diagnostics storage account using variable 'DiagStorKey' ..."
        exit 1 # terminate and indicate error
    fi
fi

set +e
kubectl -n $namespace delete secret thanos-objstore-config
# $Helm uninstall $helmReleaseName -n $namespace
set -e

sed -i "s|STOR_NAME_PLACEHOLDER|$DiagStorName|g" thanos-storage-config.yaml
sed -i "s|STOR_KEY_PLACEHOLDER|$DiagStorKey|g" thanos-storage-config.yaml

echo "Create thanos secret 'thanos-objstore-config'"
kubectl -n $namespace create secret generic thanos-objstore-config --from-file=thanos.yaml=thanos-storage-config.yaml

ThanosFlag="--set prometheus.prometheusSpec.replicas=2 --set prometheus.prometheusSpec.thanos.objectStorageConfig.key=thanos.yaml --set prometheus.prometheusSpec.thanos.objectStorageConfig.name=thanos-objstore-config "
fi

echo "************************************************************"
echo "Start helm upgrade Prometheus chart ..."
echo "************************************************************"

echo "start adding prometheus-operator-crd ..."
kubectl apply --namespace $namespace -f ./prometheus-operator-crd/monitoring.coreos.com_alertmanagerconfigs.yaml
kubectl apply --namespace $namespace -f ./prometheus-operator-crd/monitoring.coreos.com_alertmanagers.yaml
kubectl apply --namespace $namespace -f ./prometheus-operator-crd/monitoring.coreos.com_podmonitors.yaml
kubectl apply --namespace $namespace -f ./prometheus-operator-crd/monitoring.coreos.com_probes.yaml
kubectl apply --namespace $namespace -f ./prometheus-operator-crd/monitoring.coreos.com_prometheuses.yaml
kubectl apply --namespace $namespace -f ./prometheus-operator-crd/monitoring.coreos.com_prometheusrules.yaml
kubectl apply --namespace $namespace -f ./prometheus-operator-crd/monitoring.coreos.com_servicemonitors.yaml
kubectl apply --namespace $namespace -f ./prometheus-operator-crd/monitoring.coreos.com_thanosrulers.yaml

PromConfigFile=prometheus-stack-values.yaml
if [ -f bin/icm-connector-id.txt ]; then
    PromConfigFile=prometheus-stack-values-with-icm.yaml
fi

# https: //itnext.io/monitoring-kubernetes-workloads-with-prometheus-and-thanos-4ddb394b32c
echo "helm upgrade $helmReleaseName. PromConfigFile: $PromConfigFile ..."
$Helm upgrade $helmReleaseName kube-prometheus-stack-*.tgz --install --wait \
--namespace $namespace \
-f $PromConfigFile \
-f prometheus-stack-custom-values.yaml \
--set prometheusOperator.createCustomResource=false \
--set prometheusOperator.admissionWebhooks.enabled=false \
--set prometheusOperator.tlsProxy.enabled=false \
--set prometheus.prometheusSpec.externalLabels.partner_name=$PartnerName \
--set prometheus.prometheusSpec.externalLabels.environment_name=$environmentName \
--set prometheus.prometheusSpec.externalLabels.region=$compactRegion \
--set prometheus.prometheusSpec.externalLabels.aks_rg=$AKSRGName \
--set prometheus.prometheusSpec.externalLabels.aks_name=$AKSName \
--set alertmanager.alertmanagerSpec.image.repository="$liftrACRURI/prometheus/alertmanager" \
--set prometheusOperator.admissionWebhooks.patch.image.repository="$liftrACRURI/jettech/kube-webhook-certgen" \
--set prometheusOperator.image.repository="$liftrACRURI/prometheus-operator/prometheus-operator" \
--set prometheusOperator.configmapReloadImage.repository="$liftrACRURI/jimmidyson/configmap-reload" \
--set prometheusOperator.prometheusConfigReloaderImage.repository="$liftrACRURI/prometheus-operator/prometheus-config-reloader" \
--set prometheus.prometheusSpec.image.repository="$liftrACRURI/prometheus/prometheus" \
--set kube-state-metrics.image.repository="$liftrACRURI/kube-state-metrics/kube-state-metrics" \
--set prometheus-node-exporter.image.repository="$liftrACRURI/prometheus/node-exporter" \
--set prometheus.prometheusSpec.thanos.image="$liftrACRURI/thanos/thanos:v0.15.0" \
--set grafana.image.repository="$liftrACRURI/grafana/grafana" \
--set grafana.sidecar.image.repository="$liftrACRURI/kiwigrid/k8s-sidecar" \
$ThanosFlag

echo "------------------------------------------------------------"
echo "Finished helm upgrade Prometheus chart"
echo "------------------------------------------------------------"