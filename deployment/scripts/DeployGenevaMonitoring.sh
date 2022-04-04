#!/bin/bash
# Stop on error.
set -e
namespace="monitoring"

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
    --environmentName=*)
    environmentName="${i#*=}"
    shift # past argument=value
    ;;
    --compactRegion=*)
    compactRegion="${i#*=}"
    shift # past argument=value
    ;;
    --Region=*)
    Region="${i#*=}"
    shift # past argument=value
    ;;
    --gcs_region=*)
    gcs_region="${i#*=}"
    shift # past argument=value
    ;;
    --mdmVersion=*)
    mdmVersion="${i#*=}"
    shift # past argument=value
    ;;
    --mdsdVersion=*)
    mdsdVersion="${i#*=}"
    shift # past argument=value
    ;;
    --fluentdVersion=*)
    fluentdVersion="${i#*=}"
    shift # past argument=value
    ;;
    --secpackVersion=*)
    secpackVersion="${i#*=}"
    shift # past argument=value
    ;;
    --prommdmconverterVersion=*)
    prommdmconverterVersion="${i#*=}"
    shift # past argument=value
    ;;
    *)
    echo "Not matched option '${i#*=}' passed in."
    exit 1
    ;;
esac
done

if [ -z ${environmentName+x} ]; then
    echo "environmentName is blank."
    exit 1
fi

if [ -z ${DeploymentSubscriptionId+x} ]; then
    echo "DeploymentSubscriptionId is blank."
    exit 1
fi

if [ -z ${GenevaParametersFile+x} ]; then
    echo "GenevaParametersFile is blank."
    exit 1
fi

if [ -z ${Region+x} ]; then
    echo "Region is blank."
    exit 1
fi

if [ -z ${compactRegion+x} ]; then
    echo "compactRegion is blank."
    exit 1
fi

if [ -z ${gcs_region+x} ]; then
    echo "gcs_region is blank."
    exit 1
fi

if [ "$liftrcommonACRURI" = "" ]; then
    if [ -f 'bin/liftr-common-acr-endpoint.txt' ]; then
        liftrcommonACRURI=$(<bin/liftr-common-acr-endpoint.txt)
    fi
fi

if [ "$liftrcommonACRURI" != "" ]; then
    echo "liftrcommonACRURI: $liftrcommonACRURI"
fi

if [ "$liftrcommonACRURI" != "" ]; then
    if [ -z ${mdmVersion+x} ]; then
        echo "mdmVersion is blank."
        exit 1
    fi

    if [ -z ${mdsdVersion+x} ]; then
        echo "mdsdVersion is blank."
        exit 1
    fi

    if [ -z ${fluentdVersion+x} ]; then
        echo "fluentdVersion is blank."
        exit 1
    fi

    if [ -z ${secpackVersion+x} ]; then
        echo "secpackVersion is blank."
        exit 1
    fi

    if [ -z ${prommdmconverterVersion+x} ]; then
        echo "prommdmconverterVersion is blank."
        exit 1
    fi
fi

Helm="./helm"

echo "************************************************************"
echo "Start helm upgrade geneva chart ..."
echo "************************************************************"

echo "DeploymentSubscriptionId: $DeploymentSubscriptionId"
echo "GenevaParametersFile: $GenevaParametersFile"
echo "gcs_region: $gcs_region"

if [ "$PartnerName" = "" ]; then
PartnerName=$(<bin/partner-name.txt)
    if [ "$PartnerName" = "" ]; then
        echo "Please set the name of the partner using variable 'PartnerName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "PartnerName: $PartnerName"

if [ "$liftrACRURI" = "" ]; then
liftrACRURI=$(<bin/acr-endpoint.txt)
    if [ "$liftrACRURI" = "" ]; then
        echo "Please set 'liftrACRURI' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "liftrACRURI: $liftrACRURI"

if [ "$AKSRGName" = "" ]; then
AKSRGName=$(<bin/aks-rg.txt)
    if [ "$AKSRGName" = "" ]; then
        echo "Please set the name of the AKS cluster Resource Group using variable 'AKSRGName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "AKSRGName: $AKSRGName"

if [ "$AKSName" = "" ]; then
AKSName=$(<bin/aks-name.txt)
    if [ "$AKSName" = "" ]; then
        echo "Please set the name of the AKS cluster using variable 'AKSName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "AKSName: $AKSName"

if [ "$VaultName" = "" ]; then
VaultName=$(<bin/vault-name.txt)
    if [ "$VaultName" = "" ]; then
        echo "Please set the name of the Key Vault with certificates using variable 'VaultName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "VaultName: $VaultName"

if [ "$TenantId" = "" ]; then
echo "Read TenantId from file 'bin/tenant-id.txt'."
TenantId=$(<bin/tenant-id.txt)
    if [ "$TenantId" = "" ]; then
        echo "Please set the tenant Id using variable 'TenantId' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "TenantId: $TenantId"

if [ "$liftrcommonACRURI" != "" ]; then
    echo "MDMTag: $mdmVersion"
    echo "MDSDTag: $mdsdVersion"
    echo "FluentDTag: $fluentdVersion"
    echo "AzSecPackTag: $secpackVersion"
    echo "PromMDMConverterTag: $prommdmconverterVersion"
    mdmVersion=$(./GetGenevaImageVersion.sh "$mdmVersion" "$liftrcommonACRURI" "genevamdm")
    mdsdVersion=$(./GetGenevaImageVersion.sh "$mdsdVersion" "$liftrcommonACRURI" "genevamdsd")
    fluentdVersion=$(./GetGenevaImageVersion.sh "$fluentdVersion" "$liftrcommonACRURI" "genevafluentd_td-agent")
    secpackVersion=$(./GetGenevaImageVersion.sh "$secpackVersion" "$liftrcommonACRURI" "genevasecpackinstall")
    prommdmconverterVersion=$(./GetGenevaImageVersion.sh "$prommdmconverterVersion" "$liftrcommonACRURI" "shared/prom-mdm-converter")
    echo "MDMVersion: $mdmVersion"
    echo "MDSDVersion: $mdsdVersion"
    echo "FluentDVersion: $fluentdVersion"
    echo "AzSecPackVersion: $secpackVersion"
    echo "PromMDMConverterVersion: $prommdmconverterVersion"
fi

./CleanUpFirstFailedHelmRelease.sh \
--HelmReleaseName="aks-geneva" \
--Namespace=$namespace

# Deploy geneva daemonset
echo "start deploy geneva helm chart."

if [ "$liftrcommonACRURI" != "" ]; then
    $Helm upgrade aks-geneva --install --wait --force \
    --create-namespace \
    --values "$GenevaParametersFile" \
    --set keyvault="$VaultName" \
    --set tenantId="$TenantId" \
    --set genevaTenant="$PartnerName" \
    --set genevaRole="$AKSName" \
    --set hostResourceGroup="$AKSRGName" \
    --set hostRegion="$Region" \
    --set compactRegion="$compactRegion" \
    --set environmentName="$environmentName" \
    --set gcs_region="$gcs_region" \
    --set linuxgenevaACR.endpoint="$liftrcommonACRURI" \
    --set mdm.dockerTag="$mdmVersion" \
    --set mdsd.dockerTag="$mdsdVersion" \
    --set fluentd.dockerTag="$fluentdVersion" \
    --set secpack.dockerTag="$secpackVersion" \
    --set promMdmConverter.dockerTag="$prommdmconverterVersion" \
    --namespace "$namespace" geneva-*.tgz
else
    $Helm upgrade aks-geneva --install --wait --force \
    --create-namespace \
    --values "$GenevaParametersFile" \
    --set keyvault="$VaultName" \
    --set tenantId="$TenantId" \
    --set genevaTenant="$PartnerName" \
    --set genevaRole="$AKSName" \
    --set hostResourceGroup="$AKSRGName" \
    --set hostRegion="$Region" \
    --set compactRegion="$compactRegion" \
    --set environmentName="$environmentName" \
    --set gcs_region="$gcs_region" \
    --set linuxgenevaACR.endpoint="$liftrACRURI" \
    --namespace "$namespace" geneva-*.tgz
fi

echo "-------------------------------------"
echo "Finished helm upgrade geneva chart"
echo "-------------------------------------"


if [ "$liftrcommonACRURI" != "" ]; then
    echo "Starting to clear cached images which do not have a container associated with them..."
    ./ClearCachedImages.sh "$AKSName"
    echo "Cleared cached images from the VMs."
fi