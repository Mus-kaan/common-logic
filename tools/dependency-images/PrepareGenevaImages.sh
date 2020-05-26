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
echo "Latest geneva image versions: https://genevamondocs.azurewebsites.net/collect/references/linuxcontainers.html"
IMG_mdsd="genevamdsd:master_285"
IMG_mdm="genevamdm:master_37"
IMG_fluentd="genevafluentd_td-agent:master_134"
IMG_azsecpack="genevasecpackinstall:master_40"
IMG_kubegen="kube-gen:master_18"
IMG_kubectl="kubectl:master_14"
IMG_acskv="acskeyvaultagent:master_23"

ameACR="$ameLiftrACRName.azurecr.io"
msACR="$msLiftrACRName.azurecr.io"

echo "Start manageing the AME ACR ..."
echo "az login --identity"
az login --identity

echo "az account set -s $ameSubscriptionId"
az account set -s $ameSubscriptionId

echo "az group create -l $region -n $resourceGroupName"
az group create -l $region -n $resourceGroupName

echo "az acr create -n $ameLiftrACRName -g $resourceGroupName --sku Premium"
az acr create -n $ameLiftrACRName -g $resourceGroupName --sku Premium

echo "import geneva images to AME Liftr ACR"
az acr import --name $ameLiftrACRName --source $IMG_mdsd --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
az acr import --name $ameLiftrACRName --source $IMG_mdm --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
az acr import --name $ameLiftrACRName --source $IMG_fluentd --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
az acr import --name $ameLiftrACRName --source $IMG_azsecpack --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
az acr import --name $ameLiftrACRName --source $IMG_kubegen --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
az acr import --name $ameLiftrACRName --source $IMG_kubectl --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force
az acr import --name $ameLiftrACRName --source $IMG_acskv --registry /subscriptions/db67ee91-0665-44d4-b451-31faee93c5fd/resourceGroups/linuxgeneva/providers/Microsoft.ContainerRegistry/registries/linuxgeneva --force

echo "pull all the images locally"
az acr login -n $ameLiftrACRName

docker pull "$ameACR/$IMG_mdsd"
docker pull "$ameACR/$IMG_mdm"
docker pull "$ameACR/$IMG_fluentd"
docker pull "$ameACR/$IMG_azsecpack"
docker pull "$ameACR/$IMG_kubegen"
docker pull "$ameACR/$IMG_kubectl"
docker pull "$ameACR/$IMG_acskv"

echo "Start Moving Geneva images to Microsoft tenant ACR ..."
echo "Please login using your Microsoft corp credentials."
az login --use-device-code

echo "az account set -s $msSubscriptionId"
az account set -s $msSubscriptionId

echo "az group create -l $region -n $resourceGroupName"
az group create -l $region -n $resourceGroupName

echo "az acr create -n $msLiftrACRName -g $resourceGroupName --sku Premium"
az acr create -n $msLiftrACRName -g $resourceGroupName --sku Premium

echo "Psuh geneva images to MS Liftr ACR"
az acr login -n $msLiftrACRName

docker tag "$ameACR/$IMG_mdsd" "$msACR/$IMG_mdsd"
docker tag "$ameACR/$IMG_mdm" "$msACR/$IMG_mdm"
docker tag "$ameACR/$IMG_fluentd" "$msACR/$IMG_fluentd"
docker tag "$ameACR/$IMG_azsecpack" "$msACR/$IMG_azsecpack"
docker tag "$ameACR/$IMG_kubegen" "$msACR/$IMG_kubegen"
docker tag "$ameACR/$IMG_kubectl" "$msACR/$IMG_kubectl"
docker tag "$ameACR/$IMG_acskv" "$msACR/$IMG_acskv"

docker push "$msACR/$IMG_mdsd"
docker push "$msACR/$IMG_mdm"
docker push "$msACR/$IMG_fluentd"
docker push "$msACR/$IMG_azsecpack"
docker push "$msACR/$IMG_kubegen"
docker push "$msACR/$IMG_kubectl"
docker push "$msACR/$IMG_acskv"

echo "Finished preparing Geneva Images."