#!/bin/bash
# Stop on error.
set -e

Helm="./helm"
helmReleaseName="nginx-rel"
namespace="nginx"

echo "************************************************************"
echo "Start helm upgrade Nginx ingress chart ..."
echo "************************************************************"

set +e
echo "kubectl create namespace $namespace"
kubectl create namespace "$namespace"
set -e

echo "helm upgrade $helmReleaseName ..."
$Helm upgrade $helmReleaseName --install \
--namespace "$namespace" nginx-*.tgz

echo "Wait for the helm release '$helmReleaseName' ..."
kubectl rollout status deployment.apps/nginx-rel-nginx-ingress-controller -n "$namespace"
kubectl rollout status deployment.apps/nginx-rel-nginx-ingress-default-backend -n "$namespace"

echo "------------------------------------------------------------"
echo "Finished helm upgrade Nginx ingress chart"
echo "------------------------------------------------------------"