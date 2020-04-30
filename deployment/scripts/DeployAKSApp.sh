#!/bin/bash
# Stop on error.
set -e

namespace=default

for i in "$@"
do
case $i in
    --DeploymentSubscriptionId=*)
    DeploymentSubscriptionId="${i#*=}"
    shift # past argument=value
    ;; # past argument=value
    --AKSAppChartPackage=*)
    AKSAppChartPackage="${i#*=}"
    shift # past argument=value
    ;;
    --compactRegion=*)
    compactRegion="${i#*=}"
    shift # past argument=value
    ;;
    --APP_ASPNETCORE_ENVIRONMENT=*)
    APP_ASPNETCORE_ENVIRONMENT="${i#*=}"
    shift # past argument=value
    ;;
    --liftrACRURI=*)
    liftrACRURI="${i#*=}"
    shift # past argument=value
    ;;
    *)
    echo "Not matched option '${i#*=}' passed in."
    exit 1
    ;;
esac
done

if [ -z ${DeploymentSubscriptionId+x} ]; then
    echo "DeploymentSubscriptionId is blank."
    exit 1
fi

if [ -z ${AKSAppChartPackage+x} ]; then
    echo "AKSAppChartPackage is blank."
    exit 1
fi

if [ -z ${compactRegion+x} ]; then
    echo "compactRegion is blank."
    exit 1
fi

echo "AKSAppChartPackage: $AKSAppChartPackage"
echo "APP_ASPNETCORE_ENVIRONMENT: $APP_ASPNETCORE_ENVIRONMENT"

echo "************************************************************"
echo "Start helm upgrade AKS APP chart ..."
echo "************************************************************"

Helm="./helm"
echo "DeploymentSubscriptionId: $DeploymentSubscriptionId"

if [ "$liftrACRURI" = "" ]; then
echo "Read liftrACRURI from file 'bin/acr-endpoint.txt'."
liftrACRURI=$(<bin/acr-endpoint.txt)
    if [ "$liftrACRURI" = "" ]; then
        echo "Please set 'liftrACRURI' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "liftrACRURI: $liftrACRURI"

if [ "$KeyVaultEndpoint" = "" ]; then
echo "Read KeyVaultEndpoint from file 'bin/aks-kv.txt'."
KeyVaultEndpoint=$(<bin/aks-kv.txt)
    if [ "$KeyVaultEndpoint" = "" ]; then
        echo "Please set the key vault endpoint using variable 'KeyVaultEndpoint' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "KeyVaultEndpoint: $KeyVaultEndpoint"

if [ "$AppVersion" = "" ]; then
echo "Read AppVersion from file 'bin/version.txt'."
AppVersion=$(<bin/version.txt)
    if [ "$AppVersion" = "" ]; then
        echo "Please set the application version using variable 'AppVersion' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "AppVersion: $AppVersion"

if [ "$HelmReleaseName" = "" ]; then
    if test -f "bin/helm-releasename.txt"; then
        echo "Read HelmReleaseName from file 'bin/helm-releasename.txt'."
        HelmReleaseName=$(<bin/helm-releasename.txt)
    fi
    if [ "$HelmReleaseName" = "" ]; then
        echo "Variable 'HelmReleaseName' is not set. So use the default 'app-rel' ..."
        HelmReleaseName="app-rel"
    fi
fi
echo "HelmReleaseName: $HelmReleaseName"

if [ "$RPWebHostname" = "" ]; then
echo "Read RPWebHostname from file 'bin/rp-hostname.txt'."
RPWebHostname=$(<bin/rp-hostname.txt)
    if [ "$RPWebHostname" = "" ]; then
        echo "Please set svc host name using variable 'RPWebHostname' ..."
        exit 1 # terminate and indicate error
    fi
echo "expand the existing host name '$RPWebHostname' with the helm release name '$HelmReleaseName' ..."
RPWebHostname="$HelmReleaseName.$RPWebHostname"
fi
echo "RPWebHostname: $RPWebHostname"

if [ "$VaultName" = "" ]; then
echo "Read VaultName from file 'bin/vault-name.txt'."
VaultName=$(<bin/vault-name.txt)
    if [ "$VaultName" = "" ]; then
        echo "Please set the name of the Key Vault with certificates using variable 'VaultName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "VaultName: $VaultName"

./CreateCertificateSecret.sh \
--DeploymentSubscriptionId=$DeploymentSubscriptionId \
--VaultName=$VaultName \
--KeyVaultSecretName="ssl-cert" \
--tlsSecretName="aks-app-tls-secret" \
--caSecretName="aks-app-tls-ca-secret" \
--Namespace=$namespace

sslCertB64Content=$(cat ssl-cert.cer | base64 -w 0)
sslKeyB64Content=$(cat ssl-cert.key | base64 -w 0)

# Deploy the helm chart.
echo "-----------------------------------------------------------------"
echo "Start deploy '$HelmReleaseName' helm chart."
echo "-----------------------------------------------------------------"

DeploymentFlag=""
if [ "$APP_ASPNETCORE_ENVIRONMENT" = "Production" ]; then
    DeploymentFlag="--atomic --cleanup-on-fail "
    echo "Set '$DeploymentFlag' for production to roll back automatically."
fi

$Helm upgrade $HelmReleaseName --install --wait $DeploymentFlag\
--set appVersion="$AppVersion" \
--set vaultEndpoint="$KeyVaultEndpoint" \
--set hostname="$RPWebHostname" \
--set sslcertb64="$sslCertB64Content" \
--set sslkeyb64="$sslKeyB64Content" \
--set controller.service.omitClusterIP=true \
--set defaultBackend.service.omitClusterIP=true \
--set compactRegion="$compactRegion" \
--set APP_ASPNETCORE_ENVIRONMENT="$APP_ASPNETCORE_ENVIRONMENT" \
--set imageRegistry="$liftrACRURI" \
--set nginx-ingress.controller.image.repository="$liftrACRURI/kubernetes-ingress-controller/nginx-ingress-controller" \
--set nginx-ingress.defaultBackend.image.repository="$liftrACRURI/defaultbackend-amd64" \
--namespace $namespace $AKSAppChartPackage

echo "-----------------------------------------------------------------"
echo "Finished helm upgrade AKS APP chart"
echo "The web can be reached at: https://$RPWebHostname"
echo "-----------------------------------------------------------------"