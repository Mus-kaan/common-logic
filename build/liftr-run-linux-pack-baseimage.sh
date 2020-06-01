#!/bin/bash
set -e

dos2unix(){
  tr -d '\r' < "$1" > t
  mv -f t "$1"
}

SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"

echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------"
echo "Start packing Liftr image builder ..."
echo "Source root folder: $SrcRoot"
echo "CDP_FILE_VERSION_NUMERIC : $CDP_FILE_VERSION_NUMERIC"
echo "CDP_PACKAGE_VERSION_NUMERIC: $CDP_PACKAGE_VERSION_NUMERIC"
echo "CDP_FILE_VERSION_SEMANTIC: $CDP_FILE_VERSION_SEMANTIC"

PublishedImageBuilderDir="$SrcRoot/src/BaseImageBuilder/bin/publish"
SupportingFilesDir="$PublishedImageBuilderDir/supporting-files"
EV2ScriptsDir="$SupportingFilesDir/ev2-scripts"

OutDir="$SrcRoot/out-ev2-base-image"
ServiceGroupRootWindowsBaseImage="$OutDir/ServiceGroupRoot"
PackerFilesDirName="packer-files"
PackerFilesDir="$OutDir/$PackerFilesDirName"
PackerZipFileName="packer-files.zip"
PackerZipFile="$OutDir/$PackerZipFileName"
EV2ExtensionFilesDir="$OutDir/ev2-extension-files"
EV2ExtensionTarFile="$OutDir/ev2-extension.tar"
#Ev2 has a size limitation of the uploaded file(200 MB).

if [ -v CDP_PACKAGE_VERSION_NUMERIC ]; then
    EV2ArtifactVersion="$CDP_PACKAGE_VERSION_NUMERIC"
else
    # Use a fake version when building locally.
    EV2ArtifactVersion="9.9.9999-localdev"
fi
echo "EV2 artifact version is: $EV2ArtifactVersion"

# CDPx Versioning: https://onebranch.visualstudio.com/Pipeline/_wiki/wikis/Pipeline.wiki/325/Versioning
if [ -v CDP_PACKAGE_VERSION_NUMERIC ]; then
    ImageVerion="$CDP_PACKAGE_VERSION_NUMERIC"
else
    # Use a fake version when building locally.
    ImageVerion="0.3.010760009"
fi
echo "ImageVerion: $ImageVerion"

# Create directories.
mkdir --parent "$OutDir"
mkdir --parent "$PackerFilesDir"
mkdir --parent "$EV2ExtensionFilesDir"
mkdir --parent "$ServiceGroupRootWindowsBaseImage"

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Prepare packer files ..."
cp -a "$SupportingFilesDir/packer-files/." "$PackerFilesDir"

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
echo -n "$EV2ArtifactVersion" > "$EV2ExtensionFilesDir/bin/version.txt"

echo -n "$ImageVerion" > "$EV2ExtensionFilesDir/bin/numeric.packageversion.info"
echo -n "$ImageVerion" > "$EV2ExtensionFilesDir/numeric.packageversion.info"

for script in "$EV2ExtensionFilesDir"/*.sh
do
  echo "dos2unix $script"
  dos2unix $script
  chmod +x $script
done

cd "$EV2ExtensionFilesDir" && tar -cf $EV2ExtensionTarFile *

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Prepare EV2 roll out spec files ..."
cp -a "$PublishedImageBuilderDir/generated-ev2/image_builder/." "$ServiceGroupRootWindowsBaseImage"
cp $EV2ExtensionTarFile $ServiceGroupRootWindowsBaseImage
echo -n "$EV2ArtifactVersion" > "$ServiceGroupRootWindowsBaseImage/version.txt"

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Clean unecessary files ..."
rm $EV2ExtensionTarFile

echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"
echo "Finished packing base image builder"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"