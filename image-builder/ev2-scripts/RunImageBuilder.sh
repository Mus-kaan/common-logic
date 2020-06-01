#!/bin/bash

# Stop on error.
set -e

for i in "$@"
do
case $i in
    --ConfigurationPath=*)
    ConfigurationPath="${i#*=}"
    shift # past argument=value
    ;;
    --ImageName=*)
    ImageName="${i#*=}"
    shift # past argument=value
    ;;
    --ImageVersion=*)
    ImageVersion="${i#*=}"
    shift # past argument=value
    ;;
    --SourceImage=*)
    SourceImage="${i#*=}"
    shift # past argument=value
    ;;
    --RunnerSPNObjectId=*)
    RunnerSPNObjectId="${i#*=}"
    shift # past argument=value
    ;;
    --OnlyOutputSubscriptionId=*)
    OnlyOutputSubscriptionId="${i#*=}"
    shift # past argument=value
    ;;
    --ImportImage=*)
    ImportImage="${i#*=}"
    shift # past argument=value
    ;;
    --Cloud=*)
    Cloud="${i#*=}"
    shift # past argument=value
    ;;
    *)
    echo "Not matched option '${i#*=}' passed in."
    exit 1
    ;;
esac
done

if [ -z ${ConfigurationPath+x} ]; then
    echo "ConfigurationPath is blank."
    exit 1
fi

if [ -z ${ImageName+x} ]; then
    echo "ImageName is blank."
    exit 1
fi

if [ -z ${ImageVersion+x} ]; then
    echo "ImageVersion is blank."
    exit 1
fi

if [ -z ${SourceImage+x} ]; then
    echo "SourceImage is blank."
    exit 1
fi

if [ -z ${RunnerSPNObjectId+x} ]; then
    echo "RunnerSPNObjectId is blank."
    exit 1
fi

if [ -z ${OnlyOutputSubscriptionId+x} ]; then
    echo "OnlyOutputSubscriptionId is blank."
    exit 1
fi

if [ -z ${Cloud+x} ]; then
    echo "Cloud is blank."
    exit 1
fi

echo "Using Configuration file: $ConfigurationPath"

cd bin

if [ "$OnlyOutputSubscriptionId" = "true" ]; then
    dotnet BaseImageBuilder.dll \
    -f "$ConfigurationPath" \
    --outputSubscriptionIdOnly \
    -n "$ImageName" \
    -v "$ImageVersion" \
    --srcImg "$SourceImage" \
    --spnObjectId "$RunnerSPNObjectId"
elif [ "$ImportImage" = "true" ]; then
    dotnet BaseImageBuilder.dll \
    -a ImportOneVersion \
    -f "$ConfigurationPath" \
    -n "$ImageName" \
    -v "$ImageVersion" \
    --spnObjectId "$RunnerSPNObjectId" \
    --cloud "$Cloud"
else
    dotnet BaseImageBuilder.dll \
    -f "$ConfigurationPath" \
    -n "$ImageName" \
    -v "$ImageVersion" \
    --srcImg "$SourceImage" \
    --spnObjectId "$RunnerSPNObjectId" \
    --artifactPath "packer-files.zip"
fi

exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Failed to run Liftr Image Builder."
    exit $exit_code
fi