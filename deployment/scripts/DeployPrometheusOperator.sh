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

if [ "$GlobalVaultName" = "" ]; then
echo "Read GlobalVaultName from file 'bin/vault-name.txt'."
GlobalVaultName=$(<bin/global-vault-name.txt)
    if [ "$GlobalVaultName" = "" ]; then
        echo "Please set the name of the Key Vault with certificates using variable 'GlobalVaultName' ..."
        exit 1 # terminate and indicate error
    fi
fi

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

./CreateCertificateSecret.sh \
--DeploymentSubscriptionId=$DeploymentSubscriptionId \
--VaultName=$GlobalVaultName \
--KeyVaultSecretName="thanos-api" \
--tlsSecretName="dummy-tls-secret" \
--caSecretName="thanos-ca-secret" \
--Namespace=$namespace
set -e

./CreateCertificateSecret.sh \
--DeploymentSubscriptionId=$DeploymentSubscriptionId \
--VaultName=$VaultName \
--KeyVaultSecretName="ssl-cert" \
--tlsSecretName="thanos-ingress-secret" \
--caSecretName="dummy-ca-secret" \
--Namespace=$namespace



echo "************************************************************"
echo "Start helm upgrade Prometheus chart ..."
echo "************************************************************"

set +e
kubectl -n $namespace delete secret thanos-objstore-config
$Helm uninstall $helmReleaseName -n $namespace
set -e

sed -i "s|STOR_NAME_PLACEHOLDER|$DiagStorName|g" thanos-storage-config.yaml
sed -i "s|STOR_KEY_PLACEHOLDER|$DiagStorKey|g" thanos-storage-config.yaml

echo "Create thanos secret 'thanos-objstore-config'"
kubectl -n $namespace create secret generic thanos-objstore-config --from-file=thanos.yaml=thanos-storage-config.yaml

# https://itnext.io/monitoring-kubernetes-workloads-with-prometheus-and-thanos-4ddb394b32c
echo "helm upgrade $helmReleaseName ..."
$Helm upgrade $helmReleaseName prometheus-operator-*.tgz --install \
--namespace $namespace \
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
--set alertmanager.alertmanagerSpec.image.repository="$liftrACRURI/prometheus/alertmanager" \
--set prometheusOperator.tlsProxy.image.repository="$liftrACRURI/squareup/ghostunnel" \
--set prometheusOperator.admissionWebhooks.patch.image.repository="$liftrACRURI/jettech/kube-webhook-certgen" \
--set prometheusOperator.image.repository="$liftrACRURI/coreos/prometheus-operator" \
--set prometheusOperator.configmapReloadImage.repository="$liftrACRURI/coreos/configmap-reload" \
--set prometheusOperator.prometheusConfigReloaderImage.repository="$liftrACRURI/coreos/prometheus-config-reloader" \
--set prometheusOperator.hyperkubeImage.repository="$liftrACRURI/hyperkube" \
--set prometheusOperator.prometheusSpec.image.repository="$liftrACRURI/prometheus/prometheus" \
--set kube-state-metrics.image.repository="$liftrACRURI/coreos/kube-state-metrics" \
--set prometheus-node-exporter.image.repository="$liftrACRURI/prometheus/node-exporter" \

echo "Wait for the helm release '$helmReleaseName' ..."
kubectl rollout status deployment.apps/prom-rel-kube-state-metrics -n "$namespace"
kubectl rollout status deployment.apps/prom-rel-prometheus-operat-operator -n "$namespace"
kubectl rollout status daemonset/prom-rel-prometheus-node-exporter -n "$namespace"

if [ ! -f thanos-api.cer ]; then
    echo "Cannot find the api secret for Thanos. Skip deploying Thanos ingress"
else
    echo "Configuring Thanos ingress"
    kubectl apply -n "$namespace" -f thanos-sidecar-ingress.yaml
fi

echo "------------------------------------------------------------"
echo "Finished helm upgrade Prometheus chart"
echo "------------------------------------------------------------"