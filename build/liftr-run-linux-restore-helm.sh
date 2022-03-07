#!/bin/bash
set -e

echo "Starting check and restore helm client ..."
SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"
echo "SrcRoot $SrcRoot"

# download helm
HelmTarUri="https://get.helm.sh/helm-v3.2.1-linux-amd64.tar.gz"
HelmDir="$SrcRoot/buildtools/helm"
Helm="$HelmDir/linux-amd64/helm"
mkdir --parent "$HelmDir"

# Use --silent + --show-error so that curl only writes to the error stream if
# a real error occurs (OneBranch Pipeline can interpret writing an error as failure).
echo "Downloading Helm from '$HelmTarUri...'"
curl --silent --show-error "$HelmTarUri" | tar -zxC "$HelmDir"
chmod +x "$Helm"

echo "Successfully finished check and restore helm client!"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"