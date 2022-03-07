#!/bin/bash
set -e

echo "Starting check and restore crane client ..."
SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"
echo "SrcRoot $SrcRoot"

# Download crane
CraneDownloadUri="https://github.com/google/go-containerregistry/releases/download/v0.4.0/go-containerregistry_Linux_x86_64.tar.gz"
CraneDir="$SrcRoot/buildtools/crane"
CraneTar="$SrcRoot/buildtools/crane.tar.gz"
mkdir --parent "$CraneDir"

echo "Downloading crane from '$CraneDownloadUri...'"
wget -q -O $CraneTar $CraneDownloadUri
tar xzvf $CraneTar -C "$CraneDir"
rm $CraneTar
echo "Successfully finished check and restore crane client!"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"