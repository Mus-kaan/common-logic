#!/bin/bash
set -e
onebranchArtifactsFolderName="out-binary-build"

currentScriptName=`basename "$0"`

SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"
outDir="$SrcRoot/$onebranchArtifactsFolderName"
echo "Moving all build outputs to a centralized folder for OneBranch upload: $outDir"
echo "SrcRoot $SrcRoot"

mkdir --parent $outDir/exe

cp -r $SrcRoot/nupkgs $outDir
cp -r $SrcRoot/out-exe/* $outDir/exe
cp -r $SrcRoot/docker-build-input $outDir

echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"