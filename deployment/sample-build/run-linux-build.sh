#!/bin/bash

set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Starting build..."

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

dotnet build $DIR/../src/Liftr.Nginx.sln -c Release --no-restore /p:MajorVersion=$CDP_MAJOR_NUMBER_ONLY /p:MinorVersion=$CDP_MINOR_NUMBER_ONLY /p:PatchVersion=$CDP_BUILD_NUMBER /p:BuildMetadata=$CDP_DEFINITION_BUILD_COUNT

exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Failed to build"
    exit $exit_code
fi

echo "Finished build successfully"
