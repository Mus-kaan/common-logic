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

if [ "$VaultName" = "" ]; then
echo "Read VaultName from file 'bin/vault-name.txt'."
VaultName=$(<bin/vault-name.txt)
    if [ "$VaultName" = "" ]; then
        echo "Please set the name of the Key Vault with certificates using variable 'VaultName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "VaultName: $VaultName"

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

echo "************************************************************"
echo "Start helm upgrade Prometheus chart ..."
echo "************************************************************"

set +e
echo "kubectl create namespace $namespace"
kubectl create namespace "$namespace"
kubectl -n $namespace delete secret thanos-objstore-config
$Helm uninstall $helmReleaseName -n $namespace
set -e

kubectl apply -f monitoring.coreos.com_alertmanagers.yaml --validate=false
kubectl apply -f monitoring.coreos.com_prometheuses.yaml --validate=false
kubectl apply -f monitoring.coreos.com_prometheusrules.yaml --validate=false
kubectl apply -f monitoring.coreos.com_servicemonitors.yaml --validate=false
kubectl apply -f monitoring.coreos.com_podmonitors.yaml --validate=false

echo "Wait several seconds to make sure the CRDs are created ..."
sleep 5s

sed -i "s|STOR_NAME_PLACEHOLDER|$DiagStorName|g" thanos-storage-config.yaml
sed -i "s|STOR_KEY_PLACEHOLDER|$DiagStorKey|g" thanos-storage-config.yaml

echo "Create thanos secret 'thanos-objstore-config'"
kubectl -n $namespace create secret generic thanos-objstore-config --from-file=thanos.yaml=thanos-storage-config.yaml

rm thanos-storage-config.yaml
rm bin/diag-stor-key.txt

./CreateCertificateSecret.sh \
--DeploymentSubscriptionId=$DeploymentSubscriptionId \
--VaultName=$VaultName \
--KeyVaultSecretName="ssl-cert" \
--tlsSecretName="thanos-ingress-secret" \
--caSecretName="thanos-ca-secret" \
--Namespace=$namespace

# https://itnext.io/monitoring-kubernetes-workloads-with-prometheus-and-thanos-4ddb394b32c
echo "helm upgrade $helmReleaseName ..."
$Helm upgrade $helmReleaseName prometheus-operator-*.tgz --install \
--set alertmanager.enabled=false \
--set grafana.enabled=false \
--set kubeEtcd.enabled=false \
--set prometheusOperator.createCustomResource=false \
--set prometheusOperator.admissionWebhooks.enabled=false \
--set prometheusOperator.tlsProxy.enabled=false \
--set prometheus.prometheusSpec.externalLabels.partner_name=$PartnerName \
--set prometheus.prometheusSpec.externalLabels.environment_name=$environmentName \
--set prometheus.prometheusSpec.externalLabels.region=$compactRegion \
--set prometheus.prometheusSpec.externalLabels.aks_rg=$AKSRGName \
--set prometheus.prometheusSpec.externalLabels.aks_name=$AKSName \
--set prometheus.prometheusSpec.replicas=2 \
--set prometheus.prometheusSpec.retention=12h \
--set prometheus.prometheusSpec.thanos.tag=v0.3.1 \
--set prometheus.prometheusSpec.thanos.objectStorageConfig.key=thanos.yaml \
--set prometheus.prometheusSpec.thanos.objectStorageConfig.name=thanos-objstore-config \
--namespace $namespace

echo "Wait for the helm release '$helmReleaseName' ..."
kubectl rollout status deployment.apps/prom-rel-kube-state-metrics -n "$namespace"
kubectl rollout status deployment.apps/prom-rel-prometheus-operat-operator -n "$namespace"

kubectl apply -n "$namespace" -f thanos-sidecar-ingress.yaml

echo "------------------------------------------------------------"
echo "Finished helm upgrade Prometheus chart"
echo "------------------------------------------------------------"