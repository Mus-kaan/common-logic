#!/bin/bash

# Stop on error.
set -e

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
    --APP_ASPNETCORE_ENVIRONMENT=*)
    APP_ASPNETCORE_ENVIRONMENT="${i#*=}"
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
echo "AKSAppChartPackage: $AKSAppChartPackage"

if [ -z ${APP_ASPNETCORE_ENVIRONMENT+x} ]; then
APP_ASPNETCORE_ENVIRONMENT="Development"
fi
echo "APP_ASPNETCORE_ENVIRONMENT: $APP_ASPNETCORE_ENVIRONMENT"

echo "************************************************************"
echo "Start helm upgrade AKS APP chart ..."
echo "************************************************************"

Helm="./helm"
echo "DeploymentSubscriptionId: $DeploymentSubscriptionId"

if [ "$KeyVaultEndpoint" = "" ]; then
echo "Read KeyVaultEndpoint from file 'bin/aks-kv.txt'."
KeyVaultEndpoint=$(<bin/aks-kv.txt)
    if [ "$KeyVaultEndpoint" = "" ]; then
        echo "Please set the key vault endpoint using variable 'KeyVaultEndpoint' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "KeyVaultEndpoint: $KeyVaultEndpoint"

if [ "$AKSRGName" = "" ]; then
echo "Read AKSRGName from file 'bin/aks-rg.txt'."
AKSRGName=$(<bin/aks-rg.txt)
    if [ "$AKSRGName" = "" ]; then
        echo "Please set the name of the AKS cluster Resource Group using variable 'AKSRGName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "AKSRGName: $AKSRGName"

if [ "$AKSName" = "" ]; then
echo "Read AKSName from file 'bin/aks-name.txt'."
AKSName=$(<bin/aks-name.txt)
    if [ "$AKSName" = "" ]; then
        echo "Please set the name of the AKS cluster using variable 'AKSName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "AKSName: $AKSName"

if [ "$RPWebHostname" = "" ]; then
echo "Read RPWebHostname from file 'bin/rp-hostname.txt'."
RPWebHostname=$(<bin/rp-hostname.txt)
    if [ "$RPWebHostname" = "" ]; then
        echo "Please set svc host name using variable 'RPWebHostname' ..."
        exit 1 # terminate and indicate error
    fi
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

echo "az login --identity"
az login --identity
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "az login failed."
    exit $exit_code
fi

echo "az account set -s $DeploymentSubscriptionId"
az account set -s "$DeploymentSubscriptionId"
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "az account set failed."
    exit $exit_code
fi

echo "az keyvault secret download --subscription "$DeploymentSubscriptionId" --vault-name "$VaultName" --name ssl-cert --file ssl-cert-pfx"
rm -f ssl-cert-pfx
az keyvault secret download --subscription "$DeploymentSubscriptionId" --vault-name "$VaultName" --name ssl-cert --file ssl-cert-pfx
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "az keyvault secret download failed."
    exit $exit_code
fi

echo "(cat ssl-cert-pfx | base64 --decode) > ssl-cert-pfx.pfx"
rm -f ssl-cert-pfx.pfx
(cat ssl-cert-pfx | base64 --decode) > ssl-cert-pfx.pfx

# Note: Since use CA issued certificate, need to exclude CA. As https://nam06.safelinks.protection.outlook.com/?url=https%3A%2F%2Fmsazure.visualstudio.com%2FOne%2F_git%2FCompute-Runtime-Tux-GenevaContainers%3Fpath%3D%252Fdocker_geneva_mdsd_finalize%252Fstart_mdsd.sh%26version%3DGBmaster%26line%3D25%26lineStyle%3Dplain%26lineEnd%3D36%26lineStartColumn%3D1%26lineEndColumn%3D2&data=02%7C01%7CYiming.Jia%40microsoft.com%7C89c14642297342476e9e08d6b752b83e%7C72f988bf86f141af91ab2d7cd011db47%7C1%7C0%7C636897963293490981&sdata=lVisF0aQqR6NTczakYL0Uze0VWklIj9fKsGS%2FjgZicU%3D&reserved=0
# It checks mds of private key and certificate.
rm -f ssl_cert.pem
rm -f ssl_key.pem
openssl pkcs12 -in ssl-cert-pfx.pfx -out ssl_cert.pem -nodes -clcerts -nokeys -password pass:
openssl pkcs12 -in ssl-cert-pfx.pfx -out ssl_key.pem -nodes -nocerts -password pass:

sslCertB64Content=$(cat ssl_cert.pem | base64 -w 0)
sslKeyB64Content=$(cat ssl_key.pem | base64 -w 0)

rm -f ssl-cert-pfx
rm -f ssl-cert-pfx.pfx
rm -f ssl_cert.pem
rm -f ssl_key.pem

# Connect with Aks
echo "az aks get-credentials -g $AKSRGName -n $AKSName"
az aks get-credentials -g "$AKSRGName" -n "$AKSName"
$Helm repo add stable https://kubernetes-charts.storage.googleapis.com/

# Deploy the helm chart.
HelmReleaseName="liftr-rp-web-release"
echo "start deploy helm chart."
$Helm upgrade $HelmReleaseName --install --recreate-pods \
--set vaultEndpoint="$KeyVaultEndpoint" \
--set hostname="$RPWebHostname" \
--set sslcertb64="$sslCertB64Content" \
--set sslkeyb64="$sslKeyB64Content" \
--set controller.service.omitClusterIP=true \
--set defaultBackend.service.omitClusterIP=true \
--set APP_ASPNETCORE_ENVIRONMENT="$APP_ASPNETCORE_ENVIRONMENT" \
--namespace default $AKSAppChartPackage

# Wait and check Helm deployment status
# Reasons:
# 1. If the helm upgrade failed, we want the EV2 deployment to fail.
# 2. After the app is successfully deployed to AKS cluster, it will generate a public IP address. We can retrieve the IP and put it in TM in the next step.
echo "kubectl rollout status deployment/rp-web"
kubectl rollout status deployment/rp-web

echo "Wait for extra 120 seconds"
sleep 120s

echo "-----------------------------------------------------------------"
echo "Finished helm upgrade AKS APP chart"
echo "-----------------------------------------------------------------"

echo "kubectl get all"
kubectl get all