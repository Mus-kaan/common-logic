#!/bin/bash
# This script checks if the provided Geneva image tag is latest or stable. If yes, it returns the image version tag which has the same digest as this.

imageVersion=$1
acrUri=$2
repositoryName=$3
if [ "$imageVersion" = "latest"  ] || [ "$imageVersion" = "stable" ]
then
    digest=$(az acr repository show -n $acrUri --image "${repositoryName}":"${imageVersion}" --query 'digest' -o tsv)
    #echo "$digest"
    tags=$(az acr repository show-manifests -n $acrUri --repository $repositoryName --detail --query "[?digest == '$digest'].tags" -o tsv)
    #echo "$tags"
    for tag in $tags
    do
        if [ "$tag" != "$imageVersion" ]; then
            imageVersion="$tag"
            break
        fi
    done
fi
echo "$imageVersion"