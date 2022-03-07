#!/bin/bash
set -e

# print all commands for debug
# set -x

dos2unix(){
  tr -d '\r' < "$1" > t
  mv -f t "$1"
}

SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"

inputDir=$1
outputDir=$2

if [ "$inputDir" = "" ]; then
    echo "Cannot find deployment runner publish folder"
    exit 1
else
    echo "Deployment runner publish folder: $inputDir"
fi

if [ "$outputDir" = "" ]; then
    echo "Cannot find output folder"
    exit 1
else
    echo "Output folder: $outputDir"
fi

PublishedRunnerDir="$SrcRoot/$inputDir"
OutDir="$SrcRoot/$outputDir"

echo PublishedRunnerDir: $PublishedRunnerDir
echo OutDir: $OutDir

echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------"
echo "Start packing EV2 supporting files ..."
echo "Source root folder: $SrcRoot"

ServiceChartName="custom-aks-app"

Helm="$SrcRoot/buildtools/helm/linux-amd64/helm"
Crane="$SrcRoot/buildtools/crane/crane"
GenerateDockerImageMetadataDir="$SrcRoot/.onebranch-docker-metadata"
GenerateDockerImageDir="$SrcRoot/docker-images"

ChartsDir="$PublishedRunnerDir/deployment/aks/charts"
ChartsParametersDir="$PublishedRunnerDir/deployment/aks/parameters"
EV2ScriptsDir="$PublishedRunnerDir/deployment/scripts"

EV2ImportImageDir="$PublishedRunnerDir/generated-ev2/0_app_img"
EV2GlobalDir="$PublishedRunnerDir/generated-ev2/1_global"
EV2RegionalDataDir="$PublishedRunnerDir/generated-ev2/2_regional_data"
EV2RegionalComputeDir="$PublishedRunnerDir/generated-ev2/3_regional_compute"
EV2AppDir="$PublishedRunnerDir/generated-ev2/4_deploy_aks_app"
EV2TMDir="$PublishedRunnerDir/generated-ev2/5_traffic_manager"

# Output folders and files
ChartsOutDir="$OutDir/charts"
ChartsTmpDir="$OutDir/charts-tmp"
TarTmpDir="$OutDir/tar-tmp"
ExtTarFile="$OutDir/liftr-deployment.tar"

ServiceGroupRootImportImage="$OutDir/0_ServiceGroupRootImportImage"
ServiceGroupRootGlobal="$OutDir/1_ServiceGroupRootGlobal"
ServiceGroupRootRegionalData="$OutDir/2_ServiceGroupRootRegionalData"
ServiceGroupRootRegionalCompute="$OutDir/3_ServiceGroupRootRegionalCompute"
ServiceGroupRootApp="$OutDir/4_ServiceGroupRootApp"
ServiceGroupRootTM="$OutDir/5_ServiceGroupRootTrafficManager"

rm -rf $ChartsOutDir
rm -rf $ChartsTmpDir
rm -rf $TarTmpDir/*

# Set chart version based on build number.
if [[ -e "build-number.txt" ]]; then
    build_number=$(<build-number.txt)
    ChartVersion=$(echo $build_number | cut -f1 -d"-" | sed 's/.\([^.]*\)$/\1/')  # remove the dash part, e.g.2.2203.01891.35-33e3d371 -> 2.2203.01891.35 -> 2.2203.0189135
    mkdir --parent "$TarTmpDir/bin/version-files"
    echo "build_number: $build_number"
else
    # Use a fake version when building locally.
    CURRENTEPOCTIME=`date +'%y%m%d%H%M'`
    ChartVersion="0.9.$CURRENTEPOCTIME"
fi

echo "Chart version is: $ChartVersion"


if [ ! -d "$GenerateDockerImageMetadataDir" ]; then
  echo "Cannot find the Docker images metadata directory '$GenerateDockerImageMetadataDir'. Generate a fake value."
  mkdir --parent "$GenerateDockerImageMetadataDir"
  wget https://liftrfiles.blob.core.windows.net/public/20200415/gatewayWeb-metadata.json -O "$GenerateDockerImageMetadataDir/gatewayWeb-metadata.json"
  wget https://liftrfiles.blob.core.windows.net/public/20200415/sampleConsole-metadata.json -O "$GenerateDockerImageMetadataDir/sampleConsole-metadata.json"
fi

# Create directories.
mkdir --parent "$ChartsOutDir"
mkdir --parent "$TarTmpDir/bin"

echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------"
echo "Packing helm charts ..."
echo

chmod +x "$Helm"
chmod +x "$Crane"

AppChartValueFile="$ChartsTmpDir/$ServiceChartName/values.yaml"
rm -f "$AppChartValueFile"

# Package each helm chart.
for ChartDir in $ChartsDir/*; do
    ChartName="$(basename $ChartDir)"
    ChartTmpDir="$ChartsTmpDir/$ChartName"

    if [ -d "$ChartDir" ]; then
        echo "Copying Helm chart from foler '$ChartDir' to temporary directory '$ChartTmpDir'"
        mkdir --parent "$ChartTmpDir"
        cp -r $ChartDir/* "$ChartTmpDir"

        if [[ "$ChartName" = "$ServiceChartName" ]]; then
            for imgMetaData in $GenerateDockerImageMetadataDir/*.json
            do
                fileName="$(basename $imgMetaData)"
                fileName=$(echo "$fileName" | cut -f 1 -d '.' | cut -f 1 -d '-')
                echo "Parsing image meta data file: $imgMetaData"

                # Parse JSON using grep
                # See https://onebranch.visualstudio.com/OneBranch/_wiki/wikis/OneBranch.wiki/4601/Build-Docker-Images?anchor=metadata for the metadata file schema.

                DockerImageNameWithRegistry=$(cat $imgMetaData | grep -Po '"base_image_name": "\K[^"]*')

                DockerImageNameWithoutTag=$(echo $DockerImageNameWithRegistry | cut -d '/' -f1 --complement)
                ImageTag=$(cat $imgMetaData | grep -Po '"build_tag": "\K[^"]*')
                DockerImageName="$DockerImageNameWithoutTag:$ImageTag"

                echo ""                                 >> $AppChartValueFile # Ensure there is a newline at the end of the file.
                echo "$fileName:"                       >> $AppChartValueFile
                echo "  imageName: $DockerImageName"    >> $AppChartValueFile

                echo "Injecting build-specific parameters into $AppChartValueFile."
                echo "imageName: $DockerImageName"
                echo "- - - - - - - - - - - - - - - - - - - -"
            done
        fi

        echo "Packaging Helm chart at '$ChartTmpDir' ..."
        $Helm package -d "$ChartsOutDir" --version "$ChartVersion" "$ChartTmpDir"
        echo

    fi
done

echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------"
echo "Packing files for running the EV2 shell extension ..."
echo
# Create a tar of scripts for Express V2 to run.
WorkingDirectory="$( pwd )"

cp -a $PublishedRunnerDir/. "$TarTmpDir/bin"
cp -a $EV2ScriptsDir/. "$TarTmpDir"
cp -a $ChartsOutDir/. "$TarTmpDir"
cp -a $ChartsParametersDir/. "$TarTmpDir"
cp $Helm "$TarTmpDir"
cp $Crane "$TarTmpDir"
echo -n "$ChartVersion" > "$TarTmpDir/bin/version.txt"

if [ -d "$GenerateDockerImageMetadataDir" ]; then
  cp -a $GenerateDockerImageMetadataDir/. "$TarTmpDir/docker-image-metadata"
fi

for script in "$TarTmpDir"/*.sh
do
  dos2unix $script
  chmod +x $script
done

cd "$TarTmpDir" && tar -cf $ExtTarFile *
echo "Zipped scripts into $ExtTarFile."

echo
echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------"
echo "Start packing EV2 rollout specs ..."

# Create directories.
mkdir --parent "$ServiceGroupRootImportImage"
mkdir --parent "$ServiceGroupRootGlobal"
mkdir --parent "$ServiceGroupRootRegionalData"
mkdir --parent "$ServiceGroupRootRegionalCompute"
mkdir --parent "$ServiceGroupRootApp"
mkdir --parent "$ServiceGroupRootTM"

# Place deployment artifacts
cp -r $EV2GlobalDir/* $ServiceGroupRootImportImage/
cp -r $EV2GlobalDir/* $ServiceGroupRootGlobal/
cp -r $EV2RegionalDataDir/* $ServiceGroupRootRegionalData/
cp -r $EV2RegionalComputeDir/* $ServiceGroupRootRegionalCompute/
cp -r $EV2AppDir/* $ServiceGroupRootApp/
cp -r $EV2TMDir/* $ServiceGroupRootTM/

# Bundle the docker images that we will upload in EV2 shell extension
if [ -d $GenerateDockerImageDir ] 
then
    cp -r $GenerateDockerImageDir $ServiceGroupRootImportImage
    cp -r $GenerateDockerImageDir $ServiceGroupRootApp
fi

cp $ExtTarFile $ServiceGroupRootImportImage/.
cp $ExtTarFile $ServiceGroupRootGlobal/.
cp $ExtTarFile $ServiceGroupRootRegionalData/.
cp $ExtTarFile $ServiceGroupRootRegionalCompute/.
cp $ExtTarFile $ServiceGroupRootApp/.
cp $ExtTarFile $ServiceGroupRootTM/.

echo -n "$ChartVersion" > "$ServiceGroupRootImportImage/version.txt"
echo -n "$ChartVersion" > "$ServiceGroupRootGlobal/version.txt"
echo -n "$ChartVersion" > "$ServiceGroupRootRegionalData/version.txt"
echo -n "$ChartVersion" > "$ServiceGroupRootRegionalCompute/version.txt"
echo -n "$ChartVersion" > "$ServiceGroupRootApp/version.txt"
echo -n "$ChartVersion" > "$ServiceGroupRootTM/version.txt"

rm -rf $ChartsOutDir

echo "Finished packing EV2 roll out specs."
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"