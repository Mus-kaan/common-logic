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

InputEV2ScriptsDir="$SrcRoot/tools/import-geneva-images/ev2-scripts"
InputServiceRootDir="$SrcRoot/tools/import-geneva-images/ServiceGroupRoot"

OutDir="$SrcRoot/out-ev2-import-geneva"
OutServiceGroupRoot="$OutDir/ServiceGroupRoot"
EV2ExtensionFilesDir="$OutDir/ev2-extension-files"
EV2ExtensionTarFile="$OutDir/ev2-extension.tar"

# CDPx Versioning: https://onebranch.visualstudio.com/Pipeline/_wiki/wikis/Pipeline.wiki/325/Versioning
if [ -v CDP_PACKAGE_VERSION_NUMERIC ]; then
    EV2ReleaseVersion="$CDP_PACKAGE_VERSION_NUMERIC"
else
    # Use a fake version when building locally.
    CURRENTEPOCTIME=`date +'%y%m%d%H%M'`
    EV2ReleaseVersion="0.1.$CURRENTEPOCTIME"
fi
echo "EV2ReleaseVersion: $EV2ReleaseVersion"

# Create directories.
rm -rf $OutServiceGroupRoot/*
rm -rf $EV2ExtensionFilesDir/*

mkdir --parent "$OutDir"
mkdir --parent "$OutServiceGroupRoot"
mkdir --parent "$EV2ExtensionFilesDir"

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Prepare EV2 shell extension files ..."
cp -a $InputEV2ScriptsDir/. "$EV2ExtensionFilesDir"
echo -n "$EV2ReleaseVersion" > "$EV2ExtensionFilesDir/numeric.packageversion.info"
echo -n "$EV2ReleaseVersion" > "$EV2ExtensionFilesDir/version.txt"

for script in "$EV2ExtensionFilesDir"/*.sh
do
  echo "dos2unix $script"
  dos2unix $script
  chmod +x $script
done

cd "$EV2ExtensionFilesDir" && tar -cf $EV2ExtensionTarFile *

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Prepare EV2 roll out spec files ..."
cp -a "$InputServiceRootDir/." "$OutServiceGroupRoot"
cp $EV2ExtensionTarFile $OutServiceGroupRoot
echo -n "$EV2ReleaseVersion" > "$OutServiceGroupRoot/version.txt"

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "Clean unecessary files ..."
rm $EV2ExtensionTarFile

echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"
echo "Finished packing import Geneva image EV2"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"