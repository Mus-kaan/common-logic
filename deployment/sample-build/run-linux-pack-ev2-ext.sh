#!/bin/bash

dos2unix(){
  tr -d '\r' < "$1" > t
  mv -f t "$1"
}

set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Start packing EV2 ..."
echo "------------------------------------------------------------------"
echo "Start packing EV2 shell extension ..."

echo "Current folder: $DIR"

VersionFile="$DIR/../.version/numeric.fileversion.info"
Helm="$DIR/../buildtools/helm/linux-amd64/helm"
DockerImageMetadataJson="$DIR/../.metadata/image-meta.json"

PublishedRunnerDir="$DIR/../publish/Deployment.Runner"
ChartsDir="$PublishedRunnerDir/deployment/aks/charts"
ChartsParametersDir="$PublishedRunnerDir/deployment/aks/parameters"
EV2ScriptsDir="$PublishedRunnerDir/deployment/scripts"

OutDir="$DIR/../out-ev2"
ChartsOutDir="$OutDir/charts"
ChartsTmpDir="$OutDir/charts-tmp"
TarTmpDir="$OutDir/tar-tmp"
ExtTarFile="$OutDir/liftr-deployment.tar"

ServiceChartName="aks-rp-web-svc"

# Set chart version based on build number.
if [ -v CDP_PACKAGE_VERSION_NUMERIC ]; then
    ChartVersion="$CDP_PACKAGE_VERSION_NUMERIC"
    echo "Set chart version as: $ChartVersion"
else
    # Use a fake version when building locally.
    ChartVersion="9.9.9999-localdev"
fi

# Get the name of the docker image that is tagged with the build number.
if [ -f "$DockerImageMetadataJson" ]; then
    echo "Using docker build metadata at '$DockerImageMetadataJson'."

    # Use grep magic to parse JSON since jq isn't installed on the CDPx build image.
    # See https://aka.ms/cdpx/yaml/dockerbuildcommand for the metadata file schema.
    DockerImageNameWithRegistry=$(cat $DockerImageMetadataJson | grep -Po '"ame_build_image_name": "\K[^"]*')

    DockerRegistry=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1)
    DockerImageName=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 --complement)

    echo "DockerRegistry: $DockerRegistry"
    echo "DockerImageName: $DockerImageName"
else
    echo "No docker build metadata file found at '$DockerImageMetadataJson'. Docker image name will not be injected into the Helm chart."
fi

# Create directories.
mkdir --parent "$ChartsOutDir"
mkdir --parent "$TarTmpDir/bin"

# Package each helm chart.
for ChartDir in $ChartsDir/*; do
    ChartName="$(basename $ChartDir)"
    ChartTmpDir="$ChartsTmpDir/$ChartName"
    ChartValuesFile="$ChartTmpDir/values.yaml"

    echo "Copying Helm chart at $ChartDir to temporary directory $ChartTmpDir"
    mkdir --parent "$ChartTmpDir"
    cp -r $ChartDir/* "$ChartTmpDir"

    # Skip if building locally since docker image isn't known.
    if [[ "$ChartName" = "$ServiceChartName" && -v DockerRegistry ]]; then
        echo "Injecting build-specific parameters into $ChartValuesFile."
        echo ""                               >> $ChartValuesFile # Ensure there is a newline at the end of the file.
        echo "imageRegistry: $DockerRegistry" >> $ChartValuesFile
        echo "imageName: $DockerImageName"    >> $ChartValuesFile
    fi

    echo "Packaging Helm chart at $ChartTmpDir..."
    $Helm package -d "$ChartsOutDir" --version "$ChartVersion" "$ChartTmpDir"
done

# Create a tar of scripts for Express V2 to run.
WorkingDirectory="$( pwd )"

cp -a $PublishedRunnerDir/. "$TarTmpDir/bin"
cp -a $EV2ScriptsDir/. "$TarTmpDir"
cp -a $ChartsOutDir/. "$TarTmpDir"
cp -a $ChartsParametersDir/. "$TarTmpDir"
cp $Helm "$TarTmpDir"

for script in "$TarTmpDir"/*.sh
do
  dos2unix $script
  chmod +x $script
done

cd "$TarTmpDir" && tar -cf $ExtTarFile *

echo "Zipped scripts into $ExtTarFile."
echo "Finished packing EV2 shell extension."
echo "------------------------------------------------------------------"
echo "Start packing EV2 roll out specs ..."

EV2GlobalDir="$PublishedRunnerDir/deployment/ev2/global"
EV2RegionalDataDir="$PublishedRunnerDir/deployment/ev2/regional-data"
EV2RegionalComputeDir="$PublishedRunnerDir/deployment/ev2/regional-compute"
EV2AppDir="$PublishedRunnerDir/deployment/ev2/rp-web-app"

ServiceGroupRootGlobal="$OutDir/ServiceGroupRootGlobal"
ServiceGroupRootRegionalData="$OutDir/ServiceGroupRootRegionalData"
ServiceGroupRootRegionalCompute="$OutDir/ServiceGroupRootRegionalCompute"
ServiceGroupRootApp="$OutDir/ServiceGroupRootApp"

# Create directories.
mkdir --parent "$ServiceGroupRootGlobal"
mkdir --parent "$ServiceGroupRootRegionalData"
mkdir --parent "$ServiceGroupRootRegionalCompute"
mkdir --parent "$ServiceGroupRootApp"

# Place deployment artifacts
cp -r $EV2GlobalDir/* $ServiceGroupRootGlobal/
cp -r $EV2RegionalDataDir/* $ServiceGroupRootRegionalData/
cp -r $EV2RegionalComputeDir/* $ServiceGroupRootRegionalCompute/
cp -r $EV2AppDir/* $ServiceGroupRootApp/

cp $ExtTarFile $ServiceGroupRootGlobal/.
cp $ExtTarFile $ServiceGroupRootRegionalData/.
cp $ExtTarFile $ServiceGroupRootRegionalCompute/.
cp $ExtTarFile $ServiceGroupRootApp/.

if [ -f "$VersionFile" ]; then
    cp "$VersionFile" "$ServiceGroupRootGlobal/version.txt"
    cp "$VersionFile" "$ServiceGroupRootRegionalData/version.txt"
    cp "$VersionFile" "$ServiceGroupRootRegionalCompute/version.txt"
    cp "$VersionFile" "$ServiceGroupRootApp/version.txt"
else
    # Use a fake version when building locally.
    echo -n "9.9.9999" > "$ServiceGroupRootGlobal/version.txt"
    echo -n "9.9.9999" > "$ServiceGroupRootRegionalData/version.txt"
    echo -n "9.9.9999" > "$ServiceGroupRootRegionalCompute/version.txt"
    echo -n "9.9.9999" > "$ServiceGroupRootApp/version.txt"
fi

echo "Finished packing EV2 roll out specs."
echo "------------------------------------------------------------------"