#!/bin/bash
# Stop on error.
set -e

# LiftrDevTest
ameSubscriptionId="d8f298fb-60f5-4676-a7d3-25442ec5ce1e"
# Liftr - TEST
msSubscriptionId="eebfbfdb-4167-49f6-be43-466a6709609f"
resourceGroupName="liftr-acr-rg"
ameLiftrACRName="liftrameacr"
msLiftrACRName="liftrmsacr"
region="westus"

# [[[GENEVA_UPDATE_CHANGE_HERE]]]
# The version are referenced at three places. You need to update all of them. Please search for this sentence.
# latest prom mdm image version: https://msazure.visualstudio.com/Liftr/_build?definitionId=113171&_a=summary&view=runs
echo "Latest geneva image versions: https://genevamondocs.azurewebsites.net/collect/references/linuxcontainers.html"
IMG_mdsd="genevamdsd:master_20210201.2"
IMG_mdm="genevamdm:master_20210201.2"
IMG_fluentd="genevafluentd_td-agent:master_20210203.1"
IMG_azsecpack="genevasecpackinstall:master_20210201.2"
IMG_kubegen="kube-gen:master_20210201.2"
IMG_kubectl="kubectl:master_20210201.2"
IMG_acskv="acskeyvaultagent:master_20210201.2"
ameACR="$ameLiftrACRName.azurecr.io"
msACR="$msLiftrACRName.azurecr.io"

echo "az login --identity"
az login --identity

if [ "$IsFromLiftrAME2MS" = "" ]; then

    echo "Start manageing the AME ACR ..."
    echo "az account set -s $ameSubscriptionId"
    az account set -s $ameSubscriptionId

    echo "import geneva images to AME Liftr ACR"
    az acr import --name $ameLiftrACRName --source $IMG_mdsd --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
    az acr import --name $ameLiftrACRName --source $IMG_mdm --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
    az acr import --name $ameLiftrACRName --source $IMG_fluentd --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
    az acr import --name $ameLiftrACRName --source $IMG_azsecpack --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
    az acr import --name $ameLiftrACRName --source $IMG_kubegen --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
    az acr import --name $ameLiftrACRName --source $IMG_kubectl --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
    az acr import --name $ameLiftrACRName --source $IMG_acskv --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force

    echo "Finished importing Geneva images to AME Liftr ACR."

else

    echo "Start manageing the MS ACR ..."
    echo "az account set -s $msSubscriptionId"
    az account set -s $msSubscriptionId

    rm -rf ame-acr-admin-password.txt
    az keyvault secret download --subscription eebfbfdb-4167-49f6-be43-466a6709609f --vault-name liftr-acr-wus2-kv --name ame-acr-admin-password --file ame-acr-admin-password.txt
    exit_code=$?
    if [ $exit_code -ne 0 ]; then
        echo "az keyvault secret '$KeyVaultSecretName' download failed."
        exit $exit_code
    fi
    ameAdminPassword=$(cat ame-acr-admin-password.txt)
    rm -rf ame-acr-admin-password.txt

    echo "import geneva images from AME Liftr ACR to MS Liftr ACR"
    az acr import --name $msLiftrACRName --source $ameACR/$IMG_mdsd -u $ameLiftrACRName -p $ameAdminPassword --force
    az acr import --name $msLiftrACRName --source $ameACR/$IMG_mdm -u $ameLiftrACRName -p $ameAdminPassword --force
    az acr import --name $msLiftrACRName --source $ameACR/$IMG_fluentd -u $ameLiftrACRName -p $ameAdminPassword --force
    az acr import --name $msLiftrACRName --source $ameACR/$IMG_azsecpack -u $ameLiftrACRName -p $ameAdminPassword --force
    az acr import --name $msLiftrACRName --source $ameACR/$IMG_kubegen -u $ameLiftrACRName -p $ameAdminPassword --force
    az acr import --name $msLiftrACRName --source $ameACR/$IMG_kubectl -u $ameLiftrACRName -p $ameAdminPassword --force
    az acr import --name $msLiftrACRName --source $ameACR/$IMG_acskv -u $ameLiftrACRName -p $ameAdminPassword --force

    echo "Finished importing Geneva images to MS Liftr ACR."

fi
