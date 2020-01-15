#!/bin/bash
# Stop on error.
set -e

Helm="./helm"
helmReleaseName="grafana-rel"
namespace="grafana"

echo "************************************************************"
echo "Start helm upgrade Grafana chart ..."
echo "************************************************************"

set +e
echo "kubectl create namespace $namespace"
kubectl create namespace "$namespace"
set -e

echo "helm upgrade $helmReleaseName ..."
$Helm upgrade $helmReleaseName --install \
--set persistence.enabled=false \
--namespace "$namespace" grafana-*.tgz

echo "Wait for the helm release '$helmReleaseName' ..."
kubectl rollout status deployment.apps/grafana-rel -n "$namespace"

echo "------------------------------------------------------------"
echo "Finished helm upgrade Grafana chart"
echo "------------------------------------------------------------"