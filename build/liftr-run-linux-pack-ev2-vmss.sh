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

PublishedRunnerDir="$SrcRoot/src/Deployment.Runner/bin/publish"
EV2ScriptsDir="$PublishedRunnerDir/deployment/scripts"

EV2GlobalDir="$PublishedRunnerDir/generated-ev2/1_global"
EV2RegionalDataDir="$PublishedRunnerDir/generated-ev2/2_regional_data"
EV2RegionalComputeDir="$PublishedRunnerDir/generated-ev2/3_regional_compute"
EV2TMDir="$PublishedRunnerDir/generated-ev2/5_traffic_manager"

# Output folders and files
OutDir="$SrcRoot/out-ev2"
TarTmpDir="$OutDir/tar-tmp"
ExtTarFile="$OutDir/liftr-deployment.tar"

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
mkdir --parent "$ServiceGroupRootGlobal"
mkdir --parent "$ServiceGroupRootRegionalData"
mkdir --parent "$ServiceGroupRootRegionalCompute"
mkdir --parent "$ServiceGroupRootTM"

# Place deployment artifacts
cp -r $EV2GlobalDir/* $ServiceGroupRootGlobal/
cp -r $EV2RegionalDataDir/* $ServiceGroupRootRegionalData/
cp -r $EV2RegionalComputeDir/* $ServiceGroupRootRegionalCompute/
cp -r $EV2TMDir/* $ServiceGroupRootTM/

cp $ExtTarFile $ServiceGroupRootGlobal/.
cp $ExtTarFile $ServiceGroupRootRegionalData/.
cp $ExtTarFile $ServiceGroupRootRegionalCompute/.
cp $ExtTarFile $ServiceGroupRootTM/.

echo "Finished packing EV2 roll out specs."
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"