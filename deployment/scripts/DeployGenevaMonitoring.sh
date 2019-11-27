#!/bin/bash

# Script used to deploy geneva servics to Aks
# Stop on error.
set -e

for i in "$@"
do
case $i in
    --DeploymentSubscriptionId=*)
    DeploymentSubscriptionId="${i#*=}"
    shift # past argument=value
    ;;
    --GenevaParametersFile=*)
    GenevaParametersFile="${i#*=}"
    shift # past argument=value
    ;;
    --gcs_region=*)
    gcs_region="${i#*=}"
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

if [ -z ${GenevaParametersFile+x} ]; then
    echo "GenevaParametersFile is blank."
    exit 1
fi

if [ -z ${gcs_region+x} ]; then
    echo "gcs_region is blank."
    exit 1
fi

Helm="./helm"

echo "************************************************************"
echo "Start helm upgrade geneva chart ..."
echo "************************************************************"

echo "DeploymentSubscriptionId: $DeploymentSubscriptionId"
echo "GenevaParametersFile: $GenevaParametersFile"
echo "gcs_region: $gcs_region"

if [ "$liftrACRURI" = "" ]; then
echo "Read liftrACRURI from file 'bin/acr-endpoint.txt'."
liftrACRURI=$(<bin/acr-endpoint.txt)
    if [ "$liftrACRURI" = "" ]; then
        echo "Please set 'liftrACRURI' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "liftrACRURI: $liftrACRURI"

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

echo "az keyvault secret download --subscription "$DeploymentSubscriptionId" --vault-name "$VaultName" --name GenevaAMEClientCert --file encodedGenevaPfx"
az keyvault secret download --subscription "$DeploymentSubscriptionId" --vault-name "$VaultName" --name GenevaAMEClientCert --file encodedGenevaPfx
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "az keyvault secret download failed."
    exit $exit_code
fi

echo "(cat encodedGenevaPfx | base64 --decode) > geneva.pfx"
(cat encodedGenevaPfx | base64 --decode) > geneva.pfx

# Note: Since use CA issued certificate, need to exclude CA. As https://nam06.safelinks.protection.outlook.com/?url=https%3A%2F%2Fmsazure.visualstudio.com%2FOne%2F_git%2FCompute-Runtime-Tux-GenevaContainers%3Fpath%3D%252Fdocker_geneva_mdsd_finalize%252Fstart_mdsd.sh%26version%3DGBmaster%26line%3D25%26lineStyle%3Dplain%26lineEnd%3D36%26lineStartColumn%3D1%26lineEndColumn%3D2&data=02%7C01%7CYiming.Jia%40microsoft.com%7C89c14642297342476e9e08d6b752b83e%7C72f988bf86f141af91ab2d7cd011db47%7C1%7C0%7C636897963293490981&sdata=lVisF0aQqR6NTczakYL0Uze0VWklIj9fKsGS%2FjgZicU%3D&reserved=0
# It checks mds of private key and certificate.
openssl pkcs12 -in geneva.pfx -out geneva_cert.pem -nodes -clcerts -nokeys -password pass:
openssl pkcs12 -in geneva.pfx -out geneva_key.pem -nodes -nocerts -password pass:

genevaServiceCert=$(cat geneva_cert.pem | base64 -w 0)
genevaServiceKey=$(cat geneva_key.pem | base64 -w 0)

rm -f encodedGenevaPfx
rm -f geneva.pfx
rm -f geneva_cert.pem
rm -f geneva_key.pem

# Connect with Aks
echo "az aks get-credentials -g $AKSRGName -n $AKSName"
az aks get-credentials -g "$AKSRGName" -n "$AKSName"
# $Helm repo add stable https://kubernetes-charts.storage.googleapis.com/

# Deploy geneva daemonset to default namespace
echo "start deploy geneva helm chart."
$Helm upgrade aks-geneva --install --recreate-pods \
--values "$GenevaParametersFile" \
--set gcskeyb64="$genevaServiceKey" \
--set gcs_region="$gcs_region" \
--set gcscertb64="$genevaServiceCert" \
--set gcskeyb64="$genevaServiceKey" \
--set linuxgenevaACR.endpoint="$liftrACRURI" \
--set prometheus.configmapReload.image.repository="$liftrACRURI/configmap-reload" \
--set prometheus.initChownData.image.repository="$liftrACRURI/library/busybox" \
--set prometheus.kubeStateMetrics.image.repository="$liftrACRURI/coreos/kube-state-metrics" \
--set prometheus.server.image.repository="$liftrACRURI/prom/prometheus" \
--set prometheus.nodeExporter.image.repository="$liftrACRURI/prom/node-exporter" \
--namespace default geneva-*.tgz

kubectl rollout status daemonset/geneva-services

# kubectl rollout status deployment/prom-mdm-converter
# kubectl rollout status deployment/aks-geneva-prometheus-server
kubectl rollout status deployment/aks-geneva-prometheus-kube-state-metrics

echo "-------------------------------------"
echo "Finished helm upgrade geneva chart"
echo "-------------------------------------"