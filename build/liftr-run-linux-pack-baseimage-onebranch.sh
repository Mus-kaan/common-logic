#!/bin/bash
set -e

dos2unix(){
  tr -d '\r' < "$1" > t
  mv -f t "$1"
}

SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"

/bin/bash $SrcRoot/build/liftr-run-linux-restore-crane.sh

echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------"
echo "Start packing Liftr image builder ..."
echo "Source root folder: $SrcRoot"

GenerateDockerImageMetadataDir="$SrcRoot/.onebranch-docker-metadata"
GenerateDockerImageDir="$SrcRoot/docker-images"

PublishedImageBuilderDir="$SrcRoot/out-binary-build/exe/BaseImageBuilder/bin/publish"
SupportingFilesDir="$PublishedImageBuilderDir/supporting-files"
EV2ScriptsDir="$SupportingFilesDir/ev2-scripts"

OutDir="$SrcRoot/out-ev2-base-image"
ServiceGroupRoot="$OutDir/ServiceGroupRoot"
PackerFilesDirName="packer-files"
PackerFilesDir="$OutDir/$PackerFilesDirName"
PackerZipFileName="packer-files.zip"
PackerZipFile="$OutDir/$PackerZipFileName"
EV2ExtensionFilesDir="$OutDir/ev2-extension-files"
EV2ExtensionTarFile="$OutDir/ev2-extension.tar"
#Ev2 has a size limitation of the uploaded file(200 MB).

# Set version based on build number.
if [[ -e "build-number.txt" ]]; then
    build_number=$(<build-number.txt)
    ImageVerion=$(echo $build_number | sed 's/.\([^.]*\)$/\1/' | cut -d '-' -f1 )
    echo "build_number: $build_number"
else
    # Use a fake version when building locally.
    CURRENTEPOCTIME=`date +'%y%m%d%H%M'`
    ImageVerion="0.1.$CURRENTEPOCTIME"
fi

echo "ImageVerion: $ImageVerion"

# Create directories.
mkdir --parent "$OutDir"
mkdir --parent "$PackerFilesDir"
mkdir --parent "$EV2ExtensionFilesDir"
mkdir --parent "$ServiceGroupRoot"

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Prepare packer files ..."
cp -a "$SupportingFilesDir/packer-files/." "$PackerFilesDir"
chmod +x $PackerFilesDir/*.sh

cd "$OutDir"
zip -r $PackerZipFileName $PackerFilesDirName
echo "Zipped packer files into $PackerZipFile."

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Prepare EV2 shell extension files ..."
cp -a $EV2ScriptsDir/. "$EV2ExtensionFilesDir"
cp -a $PublishedImageBuilderDir/. "$EV2ExtensionFilesDir/bin"
rm -rf "$EV2ExtensionFilesDir/bin/supporting-files"
rm -rf "$EV2ExtensionFilesDir/bin/generated-ev2"
cp $PackerZipFile "$EV2ExtensionFilesDir/bin"
echo -n "$ImageVerion" > "$EV2ExtensionFilesDir/bin/version.txt"
echo -n "$ImageVerion" > "$EV2ExtensionFilesDir/bin/numeric.packageversion.info"
echo -n "$ImageVerion" > "$EV2ExtensionFilesDir/numeric.packageversion.info"

for script in "$EV2ExtensionFilesDir"/*.sh
do
  echo "dos2unix $script"
  dos2unix $script
  chmod +x $script
done

if [ -d "$GenerateDockerImageMetadataDir" ]; then
  rm -rf "$EV2ExtensionFilesDir/docker-image-metadata"
  cp -a $GenerateDockerImageMetadataDir/. "$EV2ExtensionFilesDir/docker-image-metadata"
fi

Crane="$SrcRoot/buildtools/crane/crane"
chmod +x "$Crane"
cp $Crane "$EV2ExtensionFilesDir"

cd "$EV2ExtensionFilesDir" && tar -cf $EV2ExtensionTarFile *

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Prepare EV2 roll out spec files ..."
cp -a "$PublishedImageBuilderDir/generated-ev2/image_builder/." "$ServiceGroupRoot"
cp $EV2ExtensionTarFile $ServiceGroupRoot
echo -n "$ImageVerion" > "$ServiceGroupRoot/version.txt"

# Bundle the docker images that we will upload in EV2 shell extension
if [ -d $GenerateDockerImageDir ] 
then
    cp -r $GenerateDockerImageDir $ServiceGroupRoot
fi

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Clean unecessary files ..."
rm $EV2ExtensionTarFile

echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"
echo "Finished packing base image builder"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"