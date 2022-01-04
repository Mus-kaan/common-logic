#!/bin/bash
# Stop on error.
set -e

Helm="./helm"
namespace="prometheus"

if [ "$VaultName" = "" ]; then
echo "Read VaultName from file 'bin/vault-name.txt'."
VaultName=$(<bin/vault-name.txt)
    if [ "$VaultName" = "" ]; then
        echo "Please set the name of the Key Vault with certificates using variable 'VaultName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "VaultName: $VaultName"

if [ "$DeploymentSubscriptionId" = "" ]; then
echo "Read DeploymentSubscriptionId from file 'bin/subscription-id.txt'."
DeploymentSubscriptionId=$(<bin/subscription-id.txt)
    if [ "$DeploymentSubscriptionId" = "" ]; then
        echo "Please set 'DeploymentSubscriptionId' ..."
        exit 1 # terminate and indicate error
    fi
fi

if [ "$Hostname" = "" ]; then
echo "Read Hostname from file 'bin/regional-domain-name.txt'."
Hostname=$(<bin/regional-domain-name.txt)
    if [ "$Hostname" = "" ]; then
        echo "Please set svc host name using variable 'Hostname' ..."
        exit 1 # terminate and indicate error
    fi
fi
GrafanaHostname="grafana.$Hostname"
echo "GrafanaHostname: $GrafanaHostname"
PrometheusHostname="prometheus.$Hostname"
echo "PrometheusHostname: $PrometheusHostname"

./CreateCertificateSecret.sh \
--DeploymentSubscriptionId=$DeploymentSubscriptionId \
--VaultName=$VaultName \
--KeyVaultSecretName="ssl-cert" \
--tlsSecretName="grafana-ingress-secret" \
--caSecretName="grafana-ca-secret" \
--Namespace=$namespace

echo deploy Grafana ingress
sed -i "s|INGRESS_HOSTNAME_PLACEHOLDER|$GrafanaHostname|g" grafana-ingress.yaml
kubectl apply -n $namespace -f grafana-ingress.yaml

echo deploy Prometheus ingress
sed -i "s|PROMETHEUS_INGRESS_HOSTNAME_PLACEHOLDER|$PrometheusHostname|g" prometheus-ingress.yaml
kubectl apply -n $namespace -f prometheus-ingress.yaml

echo "List all ingress in '$namespace' namespace:"
kubectl get ingress -n $namespace