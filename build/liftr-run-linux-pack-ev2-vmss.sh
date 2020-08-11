#!/bin/bash
set -e

dos2unix(){
  tr -d '\r' < "$1" > t
  mv -f t "$1"
}

SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"

echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------"
echo "Start packing VMSS EV2 supporting files ..."
echo "Source root folder: $SrcRoot"
echo "CDP_FILE_VERSION_NUMERIC : $CDP_FILE_VERSION_NUMERIC"
echo "CDP_PACKAGE_VERSION_NUMERIC: $CDP_PACKAGE_VERSION_NUMERIC"

# CDPx Versioning: https://onebranch.visualstudio.com/Pipeline/_wiki/wikis/Pipeline.wiki/325/Versioning
if [ -v CDP_PACKAGE_VERSION_NUMERIC ]; then
    ReleaseVersion="$CDP_PACKAGE_VERSION_NUMERIC"
else
    # Use a fake version when building locally.
    CURRENTEPOCTIME=`date +'%y%m%d%H%M'`
    ReleaseVersion="0.1.$CURRENTEPOCTIME"
fi
echo "ReleaseVersion: $ImageVerion"

GenerateDockerImageMetadataDir="$SrcRoot/.docker-images"

PublishedRunnerDir="$SrcRoot/src/VMSSDeployment.Runner/bin/publish"
EV2ScriptsDir="$PublishedRunnerDir/deployment/scripts"

EV2ImportImageDir="$PublishedRunnerDir/generated-ev2/0_app_img"
EV2GlobalDir="$PublishedRunnerDir/generated-ev2/1_global"
EV2RegionalDataDir="$PublishedRunnerDir/generated-ev2/2_regional_data"
EV2RegionalComputeDir="$PublishedRunnerDir/generated-ev2/3_regional_compute"
EV2TMDir="$PublishedRunnerDir/generated-ev2/5_traffic_manager"

# Output folders and files
OutDir="$SrcRoot/out-ev2-vmss"
TarTmpDir="$OutDir/tar-tmp"
ExtTarFile="$OutDir/liftr-deployment.tar"

ServiceGroupRootImportImage="$OutDir/0_ServiceGroupRootImportImage"
ServiceGroupRootGlobal="$OutDir/1_ServiceGroupRootGlobal"
ServiceGroupRootRegionalData="$OutDir/2_ServiceGroupRootRegionalData"
ServiceGroupRootRegionalCompute="$OutDir/3_ServiceGroupRootRegionalCompute"
ServiceGroupRootTM="$OutDir/5_ServiceGroupRootTrafficManager"

rm -rf $TarTmpDir/*

# Create directories.
mkdir --parent "$TarTmpDir/bin"

echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------"
echo "Packing files for running the EV2 shell extension ..."
echo
# Create a tar of scripts for Express V2 to run.
WorkingDirectory="$( pwd )"

cp -a $PublishedRunnerDir/. "$TarTmpDir/bin"
cp -a $EV2ScriptsDir/. "$TarTmpDir"

if [ -d "$GenerateDockerImageMetadataDir" ]; then
  cp -a $GenerateDockerImageMetadataDir/. "$TarTmpDir/cdpx-images"
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
mkdir --parent "$ServiceGroupRootTM"

echo -n "$ReleaseVersion" > "$ServiceGroupRootImportImage/version.txt"
echo -n "$ReleaseVersion" > "$ServiceGroupRootGlobal/version.txt"
echo -n "$ReleaseVersion" > "$ServiceGroupRootRegionalData/version.txt"
echo -n "$ReleaseVersion" > "$ServiceGroupRootRegionalCompute/version.txt"
echo -n "$ReleaseVersion" > "$ServiceGroupRootTM/version.txt"

# Place deployment artifacts
cp -r $EV2GlobalDir/* $ServiceGroupRootImportImage/
cp -r $EV2GlobalDir/* $ServiceGroupRootGlobal/
cp -r $EV2RegionalDataDir/* $ServiceGroupRootRegionalData/
cp -r $EV2RegionalComputeDir/* $ServiceGroupRootRegionalCompute/
cp -r $EV2TMDir/* $ServiceGroupRootTM/

cp $ExtTarFile $ServiceGroupRootImportImage/.
cp $ExtTarFile $ServiceGroupRootGlobal/.
cp $ExtTarFile $ServiceGroupRootRegionalData/.
cp $ExtTarFile $ServiceGroupRootRegionalCompute/.
cp $ExtTarFile $ServiceGroupRootTM/.

echo "Finished packing EV2 roll out specs."
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"