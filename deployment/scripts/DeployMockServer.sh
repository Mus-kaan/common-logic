#!/bin/bash
# Stop on error.
set -e
CurrentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
currentScriptName=`basename "$0"`

echo "CurrentDir: $CurrentDir"
echo "currentScriptName: $currentScriptName"

if [ "$APP_ASPNETCORE_ENVIRONMENT" = "" ]; then
APP_ASPNETCORE_ENVIRONMENT="Production"
fi

AKSAppChartPackage="mock-server-aks-app*.tgz"
namespace=mock
HelmReleaseName="mockapp-rel"

if [ -z ${AKSAppChartPackage+x} ]; then
    echo "AKSAppChartPackage is blank."
    exit 1
fi

echo "AKSAppChartPackage: $AKSAppChartPackage"
echo "APP_ASPNETCORE_ENVIRONMENT: $APP_ASPNETCORE_ENVIRONMENT"

echo "************************************************************"
echo "Start helm upgrade AKS APP chart for Mock Server..."
echo "************************************************************"

Helm="./helm"

if [ "$liftrACRURI" = "" ]; then
echo "Read liftrACRURI from file 'bin/acr-endpoint.txt'."
liftrACRURI=$(<bin/acr-endpoint.txt)
    if [ "$liftrACRURI" = "" ]; then
        echo "Please set 'liftrACRURI' ..."
        exit 1 # terminate and indicate error
    fi
fi

echo "liftrACRURI: $liftrACRURI"
echo "HelmReleaseName: $HelmReleaseName"

# Deploy the helm chart.
echo "-----------------------------------------------------------------"
echo "Start deploy '$HelmReleaseName' helm chart."
echo "-----------------------------------------------------------------"

# https://github.com/helm/helm/issues/3353

./CleanUpFirstFailedHelmRelease.sh \
--HelmReleaseName=$HelmReleaseName \
--Namespace=$namespace

$Helm upgrade $HelmReleaseName --install --wait --create-namespace --force \
--set imageRegistry="$liftrACRURI" \
--namespace $namespace $AKSAppChartPackage

echo "-----------------------------------------------------------------"
echo "Finished helm upgrade AKS APP chart for Mock Server"
echo "-----------------------------------------------------------------"