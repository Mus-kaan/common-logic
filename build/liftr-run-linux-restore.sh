#!/bin/bash
set -e

echo "Starting restore dependency packages ..."
SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"
echo "SrcRoot $SrcRoot"
# echo "CDP_DEFAULT_CLIENT_PACKAGE_PAT: $CDP_DEFAULT_CLIENT_PACKAGE_PAT"

if [ ! -f "$SrcRoot/buildtools/reportgenerator" ]; then
    dotnet tool install dotnet-reportgenerator-globaltool --tool-path $SrcRoot/buildtools
fi

for solution in $SrcRoot/src/*.sln
do
  echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------"
  echo "Start dotnet restore $solution"
  echo

  dotnet restore $solution -v minimal
    exit_code=$?
    if [ $exit_code -ne 0 ]; then
        echo "Failed to restore"
        exit $exit_code
    fi
  echo "Finished dotnet restore $solution"
  echo "==========[Liftr]==========[https://aka.ms/liftr]==========[Liftr]==========[https://aka.ms/liftr]=========="
done

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

# https://github.com/domaindrivendev/Swashbuckle.AspNetCore#swashbuckleaspnetcorecli
cd $SrcRoot/src/Samples/Liftr.Sample.Web
dotnet tool restore

echo "Successfully finished package restore!"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"