#!/bin/bash
set -e

targetTagName=$1

# https://docs.microsoft.com/en-us/azure/virtual-machines/windows/instance-metadata-service#vm-tags
tagList=$(curl -H "Metadata:true" --silent "http://169.254.169.254/metadata/instance/compute/tagsList?api-version=2019-06-04")

tagObjectList=$(echo "$tagList" | jq -c '.[]')

for tagKvp in ${tagObjectList}
do
	# echo "Parsing tagKvp: $tagKvp"
    tagName=$(echo "$tagKvp" | jq -r '.name')
    tagValue=$(echo "$tagKvp" | jq -r '.value')
    if  [[ $tagName =~ ^$targetTagName ]] ;
    then
        echo $tagValue
    fi
done