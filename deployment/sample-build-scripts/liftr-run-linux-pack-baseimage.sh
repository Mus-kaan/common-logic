#!/bin/bash
set -e

dos2unix(){
  tr -d '\r' < "$1" > t
  mv -f t "$1"
}

SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"

echo "----------[Liftr]----------[Liftr]----------[Liftr]----------[Liftr]----------"
echo "Start packing base image builder ..."
echo "Source root folder: $SrcRoot"
echo "CDP_FILE_VERSION_NUMERIC : $CDP_FILE_VERSION_NUMERIC"
echo "CDP_PACKAGE_VERSION_NUMERIC: $CDP_PACKAGE_VERSION_NUMERIC"

SemanticVersionFile="$SrcRoot/.version/semantic.fileversion.info"
BaseImageDir="$SrcRoot/base-image"
PakcerScriptsDir="$BaseImageDir/packer-scripts"
EV2ScriptsDir="$BaseImageDir/ev2-scripts"
EV2SIGProvisionDir="$BaseImageDir/ev2/sig-provision"
EV2BakeSBIDir="$BaseImageDir/ev2/bake-base-image"
GenevaConfigDir="$BaseImageDir/genevaConfig"
PublishedSBIBuilderDir="$SrcRoot/src/Liftr.BaseImageBuilder/bin/publish"

DockerImageMetadataJson="$SrcRoot/.docker-images/gatewayWeb.json"

OutDir="$SrcRoot/out-base-image"
ServiceGroupRootSIG="$OutDir/ServiceGroupRootSIG"
ServiceGroupRootWBI="$OutDir/ServiceGroupRootWBI"
PackerFilesDir="$OutDir/Packer-files"
EV2ExtensionFilesDir="$OutDir/Ev2-extension-files"
#Ev2 has a size limitation of the uploaded file(200 MB).
EV2ExtensionTarFile="$OutDir/Ev2-extension.tar"

if [ -v CDP_PACKAGE_VERSION_NUMERIC ]; then
    EV2ArtifactVersion="$CDP_PACKAGE_VERSION_NUMERIC"
else
    # Use a fake version when building locally.
    EV2ArtifactVersion="9.9.9999-localdev"
fi
echo "EV2 artifact version is: $EV2ArtifactVersion"

  
if [ ! -f "$SemanticVersionFile" ]; then
    echo "Generate a fake version file at '$SemanticVersionFile'."
    echo "0.9.01076.0009-417de0e5" >> $SemanticVersionFile
fi

# Create directories.
mkdir --parent "$OutDir"
mkdir --parent "$PackerFilesDir"
mkdir --parent "$EV2ExtensionFilesDir"
mkdir --parent "$ServiceGroupRootSIG"
mkdir --parent "$ServiceGroupRootWBI"

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Prepare packer files ..."

cp -a "$PakcerScriptsDir/." "$PackerFilesDir"

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Prepare EV2 shell extension files ..."

cp -a $GenevaConfigDir/. "$EV2ExtensionFilesDir/bin"
cp -a $PakcerScriptsDir/. "$EV2ExtensionFilesDir/bin"
cp -a $PublishedSBIBuilderDir/. "$EV2ExtensionFilesDir/bin"
cp -a $PackerFilesDir "$EV2ExtensionFilesDir/bin"
cp -a $EV2ScriptsDir/. "$EV2ExtensionFilesDir"

echo "Using version file at '$SemanticVersionFile'."
cp $SemanticVersionFile "$EV2ExtensionFilesDir/bin"
cp $SemanticVersionFile "$EV2ExtensionFilesDir"

for script in "$EV2ExtensionFilesDir"/*.sh
do
  echo "dos2unix $script"
  dos2unix $script
  chmod +x $script
done

cd "$EV2ExtensionFilesDir" && tar -cf $EV2ExtensionTarFile *

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Prepare EV2 roll out spec files ..."
cp -r $EV2SIGProvisionDir/* $ServiceGroupRootSIG/
cp -r $EV2BakeSBIDir/* $ServiceGroupRootWBI/

cp $EV2ExtensionTarFile $ServiceGroupRootSIG/.
cp $EV2ExtensionTarFile $ServiceGroupRootWBI/.

echo -n "$EV2ArtifactVersion" > "$ServiceGroupRootSIG/version.txt"
echo -n "$EV2ArtifactVersion" > "$ServiceGroupRootWBI/version.txt"

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Clean unecessary files ..."
rm $EV2ExtensionTarFile

echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"
echo "Finished packing base image builder"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"