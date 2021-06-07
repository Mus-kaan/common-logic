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

if [ ! -f bin/thanos-client-ip.txt ]; then
    echo "Skip deploying Thanos ingress due to cannot find 'bin/thanos-client-ip.txt'"
    exit 0
fi

if [ "$VaultName" = "" ]; then
echo "Read VaultName from file 'bin/vault-name.txt'."
VaultName=$(<bin/vault-name.txt)
    if [ "$VaultName" = "" ]; then
        echo "Please set the name of the Key Vault with certificates using variable 'VaultName' ..."
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

echo "Read Thanos client IP range from file 'bin/thanos-client-ip.txt'."
ThanosClientIPRange=$(<bin/thanos-client-ip.txt)
if [ "$ThanosClientIPRange" = "" ]; then
    echo "Cannot find ThanosClientIPRange ..."
    exit 1 # terminate and indicate error
fi
echo "ThanosClientIPRange: $ThanosClientIPRange"
sed -i "s|PLACE_HOLDER_THANOS_CLIENT_IP_RANGE|$ThanosClientIPRange|g" thanos-sidecar-ingress.yaml

./CreateCertificateSecret.sh \
--DeploymentSubscriptionId=$DeploymentSubscriptionId \
--VaultName=$VaultName \
--KeyVaultSecretName="ssl-cert" \
--tlsSecretName="thanos-ingress-secret" \
--caSecretName="dummy-ca-secret" \
--Namespace=$namespace

echo "************************************************************"
echo "Start deploy Thanos ingress ..."
echo "************************************************************"

kubectl apply -n "$namespace" -f thanos-sidecar-ingress.yaml

echo "------------------------------------------------------------"
echo "Finished deploy Thanos ingress"
echo "------------------------------------------------------------"