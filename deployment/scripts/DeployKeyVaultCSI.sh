#!/bin/bash
# Stop on error.
set -e

Helm="./helm"
helmReleaseName="akv-csi-rel"
namespace="kv-csi"

echo "************************************************************"
echo "Start helm deploying Azure Key Vault Provider for Secrets Store CSI Driver"
echo "************************************************************"
# If intermediate certificates should be specified in addition to a primary certificate,
#  they should be specified in the same file in the following order: the primary certificate
#  comes first, then the intermediate certificates.
# http://nginx.org/en/docs/http/ngx_http_ssl_module.html#ssl_certificate
$Helm upgrade $helmReleaseName --install --wait --timeout 10m \
--create-namespace \
--set constructPEMChain=true \
--set secrets-store-csi-driver.enableSecretRotation=true \
--set secrets-store-csi-driver.rotationPollInterval=30m \
--namespace "$namespace" csi-secrets-store-provider-azure-*.tgz

echo "------------------------------------------------------------"
echo "Finished helm deploy Key Vault CSI"
echo "------------------------------------------------------------"

echo Add pod security policy
kubectl apply --namespace $namespace -f ./pod-security-policy.yaml