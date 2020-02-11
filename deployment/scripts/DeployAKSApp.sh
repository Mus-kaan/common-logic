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
echo "start deploy $HelmReleaseName helm chart."
$Helm upgrade $HelmReleaseName --install \
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

# Wait and check Helm deployment status
# Reasons:
# 1. If the helm upgrade failed, we want the EV2 deployment to fail.
# 2. After the app is successfully deployed to AKS cluster, it will generate a public IP address. We can retrieve the IP and put it in TM in the next step.

echo "Getting names of the current deployments in Kubernetes"
deploymentNames=$(kubectl get deployments -o=jsonpath='{.items[*].metadata.name}' |tr -s '[[:space:]]' '\n')

echo "Name of the deployments are":
echo $deploymentNames

for name in $deploymentNames
do
    ROLLOUT_STATUS_CMD="kubectl rollout status deployment/$name --timeout=600s" #wait for 10 minutes for the deployment to succeed
    echo "Checking rollout status for deployment $name"
    $ROLLOUT_STATUS_CMD || exit 1 #exit statement will only be executed when the command returns a non zero which implies a failure
done

echo "-----------------------------------------------------------------"
echo "Finished helm upgrade AKS APP chart"
echo "The web can be reached at: https://$RPWebHostname"
echo "-----------------------------------------------------------------"