#!/bin/bash

set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Starting pakcage restore"

dotnet restore $DIR/../src/Liftr.Nginx.sln -v minimal
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Failed to restore"
    exit $exit_code
fi

# download helm
HelmTarUri="https://get.helm.sh/helm-v3.0.0-beta.3-linux-amd64.tar.gz"
HelmDir="$DIR/../buildtools/helm"
Helm="$HelmDir/linux-amd64/helm"
ChartsDir="$DIR/../deployment/aks/charts"
echo "------------------------------------------------------------------"
echo "Downloading Helm for from '$HelmTarUri...'"
echo "------------------------------------------------------------------"
mkdir --parent "$HelmDir"

# Use --silent + --show-error so that curl only writes to the error stream if
# a real error occurs (OneBranch Pipeline can interpret writing an error as failure).
curl --silent --show-error "$HelmTarUri" | tar -zxC "$HelmDir"
chmod +x "$Helm"

echo "Finished pakcage restore successfully"
