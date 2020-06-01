#!/bin/bash
set -e
currentScriptName=`basename "$0"`

echo "Starting building code ..."
SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"
echo "SrcRoot $SrcRoot"

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

for solution in $SrcRoot/src/*.sln
do
  echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------"
  echo "Start dotnet build $solution"
  dotnet build $solution -c Release --no-restore /p:MajorVersion=$CDP_MAJOR_NUMBER_ONLY /p:MinorVersion=$CDP_MINOR_NUMBER_ONLY /p:PatchVersion=$CDP_BUILD_NUMBER /p:BuildMetadata=$CDP_DEFINITION_BUILD_COUNT
    exit_code=$?
    if [ $exit_code -ne 0 ]; then
        echo "Failed to build"
        exit $exit_code
    fi
  echo "Finished dotnet build $solution"
  echo "==========[Liftr]==========[https://aka.ms/liftr]==========[Liftr]==========[https://aka.ms/liftr]=========="
done

echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"