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

echo "helm upgrade $helmReleaseName ..."

if [ "$PublicIP" = "" ]; then
$Helm upgrade $helmReleaseName --install --wait --timeout 25m \
--create-namespace \
-f nginx-values.yaml \
--set controller.image.repository="$liftrACRURI/ingress-nginx/controller" \
--set controller.admissionWebhooks.patch.image.repository="$liftrACRURI/ingress-nginx/kube-webhook-certgen" \
--set controller.service.enableHttp=false \
--set controller.service.externalTrafficPolicy=Local \
--set controller.addHeaders.X-Content-Type-Options="nosniff" \
--set defaultBackend.image.repository="$liftrACRURI/defaultbackend-amd64" \
--namespace "$namespace" ingress-nginx-*.tgz
else
$Helm upgrade $helmReleaseName --install --wait --timeout 25m \
--create-namespace \
-f nginx-values.yaml \
--set controller.image.repository="$liftrACRURI/ingress-nginx/controller" \
--set controller.admissionWebhooks.patch.image.repository="$liftrACRURI/ingress-nginx/kube-webhook-certgen" \
--set controller.service.enableHttp=false \
--set controller.service.externalTrafficPolicy=Local \
--set controller.addHeaders.X-Content-Type-Options="nosniff" \
--set defaultBackend.image.repository="$liftrACRURI/defaultbackend-amd64" \
--set controller.service.loadBalancerIP=$PublicIP \
--set controller.service.annotations."service\.beta\.kubernetes\.io\/azure\-load\-balancer\-resource\-group"=$PublicIPRG \
--namespace "$namespace" ingress-nginx-*.tgz
fi

# Static IP documentation: https://docs.microsoft.com/en-us/azure/aks/static-ip


echo "------------------------------------------------------------"
echo "Finished helm upgrade Nginx ingress chart"
echo "------------------------------------------------------------"