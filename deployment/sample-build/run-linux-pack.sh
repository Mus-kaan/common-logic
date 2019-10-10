#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Packing ..."

if [ -z "$CDP_MAJOR_NUMBER_ONLY" ]; then
    CDP_MAJOR_NUMBER_ONLY=0
fi
if [ -z "$CDP_MINOR_NUMBER_ONLY" ]; then
    CDP_MINOR_NUMBER_ONLY=1
fi
if [ -z "$CDP_BUILD_NUMBER" ]; then
    CDP_BUILD_NUMBER=1
fi
if [ -z "$CDP_DEFINITION_BUILD_COUNT" ]; then
    CDP_DEFINITION_BUILD_COUNT=2
fi

echo "Major version number: $CDP_MAJOR_NUMBER_ONLY"
echo "Minor version number: $CDP_MINOR_NUMBER_ONLY"
echo "Build number: $CDP_BUILD_NUMBER"
echo "CDP_DEFINITION_BUILD_COUNT: $CDP_DEFINITION_BUILD_COUNT"

dotnet pack $DIR/../src/Liftr.Nginx.sln -c Release --include-source --include-symbols --no-build --no-restore -o $DIR/../nupkgs /p:MajorVersion=$CDP_MAJOR_NUMBER_ONLY /p:MinorVersion=$CDP_MINOR_NUMBER_ONLY /p:PatchVersion=$CDP_BUILD_NUMBER /p:BuildMetadata=$CDP_DEFINITION_BUILD_COUNT

exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Failed to pack"
    exit $exit_code
fi
echo "Finished packing nugets successfully"

dotnet publish $DIR/../src/Services/Liftr.Nginx.RP.Web/Liftr.Nginx.RP.Web.csproj -c Release --no-build --no-restore -o $DIR/../src/Services/Liftr.Nginx.RP.Web/publish /p:MajorVersion=$CDP_MAJOR_NUMBER_ONLY /p:MinorVersion=$CDP_MINOR_NUMBER_ONLY /p:PatchVersion=$CDP_BUILD_NUMBER /p:BuildMetadata=$CDP_DEFINITION_BUILD_COUNT

dotnet publish $DIR/../src/Services/Deployment.Runner/Deployment.Runner.csproj -c Release --no-build --no-restore -o $DIR/../publish/Deployment.Runner /p:MajorVersion=$CDP_MAJOR_NUMBER_ONLY /p:MinorVersion=$CDP_MINOR_NUMBER_ONLY /p:PatchVersion=$CDP_BUILD_NUMBER /p:BuildMetadata=$CDP_DEFINITION_BUILD_COUNT

exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Failed to publish"
    exit $exit_code
fi