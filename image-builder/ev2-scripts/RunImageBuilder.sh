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
    --ImageVersionTag=*)
    ImageVersionTag="${i#*=}"
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

if [ -z ${ImageVersionTag+x} ]; then
    echo "ImageVersionTag is blank."
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


echo "Using Configuration file: $ConfigurationPath"

cd bin

if [ "$OnlyOutputSubscriptionId" = "true" ]; then
    dotnet BaseImageBuilder.dll \
    -f "$ConfigurationPath" \
    --outputSubscriptionIdOnly \
    -n "$ImageName" \
    --imageVersionTag "$ImageVersionTag" \
    --srcImg "$SourceImage" \
    --spnObjectId "$RunnerSPNObjectId" \
    --artifactPath "packer-files.tar.gz"
else
    dotnet BaseImageBuilder.dll \
    -f "$ConfigurationPath" \
    -n "$ImageName" \
    --imageVersionTag "$ImageVersionTag" \
    --srcImg "$SourceImage" \
    --spnObjectId "$RunnerSPNObjectId" \
    --artifactPath "packer-files.tar.gz"
fi

exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Failed to run Liftr Image Builder."
    exit $exit_code
fi

echo "----------------------------------------------------------------------------------------------"
echo "Finished running the Liftr Image Builder."
echo "----------------------------------------------------------------------------------------------"