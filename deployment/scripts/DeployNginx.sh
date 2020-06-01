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

PublicIP=""
if [ -f "bin/public-ip.txt" ]; then
PublicIP=$(<bin/public-ip.txt)
echo "PublicIP: $PublicIP"

PublicIPRG=$(<bin/public-ip-rg.txt)
echo "PublicIPRG: $PublicIPRG"
fi

set +e
echo "kubectl create namespace $namespace"
kubectl create namespace "$namespace"
set -e

echo "helm upgrade $helmReleaseName ..."

if [ "$PublicIP" = "" ]; then
$Helm upgrade $helmReleaseName --install --wait --timeout 25m \
--set controller.image.repository="$liftrACRURI/kubernetes-ingress-controller/nginx-ingress-controller" \
--set controller.admissionWebhooks.patch.image.repository="$liftrACRURI/jettech/kube-webhook-certgen" \
--set controller.service.enableHttp=false \
--set defaultBackend.image.repository="$liftrACRURI/defaultbackend-amd64" \
--namespace "$namespace" nginx-*.tgz
else
$Helm upgrade $helmReleaseName --install --wait --timeout 25m \
--set controller.image.repository="$liftrACRURI/kubernetes-ingress-controller/nginx-ingress-controller" \
--set controller.admissionWebhooks.patch.image.repository="$liftrACRURI/jettech/kube-webhook-certgen" \
--set controller.service.enableHttp=false \
--set defaultBackend.image.repository="$liftrACRURI/defaultbackend-amd64" \
--set controller.service.loadBalancerIP=$PublicIP \
--set controller.service.annotations."service\.beta\.kubernetes\.io\/azure\-load\-balancer\-resource\-group"=$PublicIPRG \
--namespace "$namespace" nginx-*.tgz
fi

# Static IP documentation: https://docs.microsoft.com/en-us/azure/aks/static-ip


echo "------------------------------------------------------------"
echo "Finished helm upgrade Nginx ingress chart"
echo "------------------------------------------------------------"