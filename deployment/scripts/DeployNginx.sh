#!/bin/bash
# Stop on error.
set -e

Helm="./helm"
helmReleaseName="nginx-rel"
namespace="nginx"

echo "************************************************************"
echo "Start helm upgrade Nginx ingress chart ..."
echo "************************************************************"

if [ "$liftrACRURI" = "" ]; then
liftrACRURI=$(<bin/acr-endpoint.txt)
    if [ "$liftrACRURI" = "" ]; then
        echo "Please set 'liftrACRURI' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "liftrACRURI: $liftrACRURI"

set +e
echo "kubectl create namespace $namespace"
kubectl create namespace "$namespace"
set -e

echo "helm upgrade $helmReleaseName ..."
$Helm upgrade $helmReleaseName --install --atomic --wait --cleanup-on-fail --timeout 15m \
--set controller.image.repository="$liftrACRURI/kubernetes-ingress-controller/nginx-ingress-controller" \
--set controller.admissionWebhooks.patch.image.repository="$liftrACRURI/jettech/kube-webhook-certgen" \
--set defaultBackend.image.repository="$liftrACRURI/defaultbackend-amd64" \
--namespace "$namespace" nginx-*.tgz

echo "------------------------------------------------------------"
echo "Finished helm upgrade Nginx ingress chart"
echo "------------------------------------------------------------"