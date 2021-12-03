#!/bin/bash
# Stop on error.
set -e

Helm="./helm"
helmReleaseName="keda-rel"
namespace="keda"

if [ "$liftrACRURI" = "" ]; then
liftrACRURI=$(<bin/acr-endpoint.txt)
    if [ "$liftrACRURI" = "" ]; then
        echo "Please set 'liftrACRURI' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "liftrACRURI: $liftrACRURI"

echo "************************************************************"
echo "Start helm deploying KEDA (https://keda.sh/)"
echo "************************************************************"
$Helm upgrade $helmReleaseName --install --wait --timeout 10m \
--create-namespace \
--set image.keda.repository="$liftrACRURI/kedacore/keda" \
--set image.metricsApiServer.repository="$liftrACRURI/kedacore/keda-metrics-apiserver" \
--namespace "$namespace" keda-*.tgz

echo "------------------------------------------------------------"
echo "Finished helm deploy KEDA (https://keda.sh/)"
echo "------------------------------------------------------------"