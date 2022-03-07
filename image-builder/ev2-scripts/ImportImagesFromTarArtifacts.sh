#!/bin/bash
# Stop on error.
set -e

CurrentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [ "$NoWait" = "true" ]; then
    echo "Skip import images when 'NoWait' is set"
    exit 0
fi

echo "Read DeploymentSubscriptionId from file 'bin/subscription-id.txt'."
DeploymentSubscriptionId=$(<bin/subscription-id.txt)
if [ "$DeploymentSubscriptionId" = "" ]; then
    echo "Please set 'DeploymentSubscriptionId' ..."
    exit 1 # terminate and indicate error
fi
echo "DeploymentSubscriptionId: $DeploymentSubscriptionId"

echo "Read ACRName from file 'bin/acr-name.txt'."
ACRName=$(<bin/acr-name.txt)
if [ "$ACRName" = "" ]; then
    echo "Please set 'ACRName' ..."
    exit 1 # terminate and indicate error
fi
echo "ACRName: $ACRName"

echo "Read TenantId from file 'bin/tenant-id.txt'."
TenantId=$(<bin/tenant-id.txt)
if [ "$TenantId" = "" ]; then
    echo "Please set 'TenantId' ..."
    exit 1 # terminate and indicate error
fi
echo "TenantId: $TenantId"

ImageMetadataDir="$CurrentDir/docker-image-metadata"
if [ ! -d "$ImageMetadataDir" ]; then
    echo "Docker image metadata not available"
    exit 1
fi

echo "Docker images meta data folder: $ImageMetadataDir"


Crane="./crane"

echo "Getting ACR credentials"
TokenQueryRes=$(az acr login -n "$ACRName" -t)       
Token=$(echo "$TokenQueryRes" | grep -Po '"accessToken": "\K[^"]*')
DestinationACR=$(echo "$TokenQueryRes" | grep -Po '"loginServer": "\K[^"]*')
echo $Token | $Crane auth login "$DestinationACR" -u "00000000-0000-0000-0000-000000000000" --password-stdin

for imgMetaData in $ImageMetadataDir/*.json
do
    fileName="$(basename $imgMetaData)"
    fileName=$(echo "$fileName" | cut -f 1 -d '.')
    echo "Parsing image meta data file: $imgMetaData"

    # Use grep magic to parse JSON since jq isn't installed on the release image.
    # See https://onebranch.visualstudio.com/OneBranch/_wiki/wikis/OneBranch.wiki/4601/Build-Docker-Images?anchor=metadata for the metadata file schema.

    DockerImageNameWithRegistry=$(cat $imgMetaData | grep -Po '"base_image_name": "\K[^"]*')
    DockerImageName=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 --complement)

    Repository=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 --complement)
    DockerImageName=$(echo $Repository | cut -d '/' -f1 --complement | cut -d '/' -f1 --complement)
    Tag=$(cat $imgMetaData | grep -Po '"build_tag": "\K[^"]*')

    echo "DockerImageName: $DockerImageName"
    echo "Repository: $Repository"
    echo "Tag: $Tag"

    GivenRepositoryName=$(cat $imgMetaData | grep -Po '"repository_name": "\K[^"]*')

    echo "Check existing tags for repository $Repository"
    set +e
    existingTags=$(az acr repository show-tags --name $ACRName --repository $Repository)
    set -e
    
    if echo "$existingTags" | grep $Tag; then
        echo "$Repository:$Tag already exist, skip import."
    else
        DestImageFullName="$DestinationACR/$Repository:$Tag"
        echo "DestImageFullName: $DestImageFullName"
        
        echo "$Repository:$Tag does not exist. Upload image from tarball/artifact to $DestImageFullName"

        TarImagePath="$GivenRepositoryName.tar.gz"

        #The environment variable to the path for docker image is the same as the repository name with the '-' removed since bash does not allow this in vars
        TarPathEnvVar=$(echo $GivenRepositoryName | sed "s/-//g")

        #Parse through env variables to get SAS URIs for the tar images since they are dynamically generated in the build stage
        for var in $(compgen -e); do
   
            if [[ "$var" == "$TarPathEnvVar" ]]; then
                TarballImageFileSAS=${!var}
                #echo "Downloading docker tarball image from $TarballImageFileSAS"
                wget -O $TarImagePath "$TarballImageFileSAS"
            fi
        done
        
        if [ -f "$TarImagePath" ]; then
            echo "Docker push $TarImagePath to $DestinationACR"
            gunzip $TarImagePath
            $Crane push "$GivenRepositoryName.tar" $DestImageFullName
        fi
    fi
done