#!/bin/bash
set -e

publishProject(){
  publishOrNot="false"
  hasDockerFile="false"

  csproj=$1
  rootPath=$2
  csprojFolder="$(dirname "$csproj")"
  csprojFolderName="$(basename $(dirname "$csproj"))"

  if grep -q "<OutputType>Exe</OutputType>" "$csproj"; then
    echo "Found console project to publish: $csproj"
    publishOrNot="true"
  elif grep -q "Microsoft.NET.Sdk.Web" "$csproj"; then
    echo "Found web project to publish: $csproj"
    publishOrNot="true"
  elif grep -q "Microsoft.NET.Sdk.Worker" "$csproj"; then
    echo "Found worker service project to publish: $csproj"
    publishOrNot="true"
  fi

  publishFolder=$rootPath/out-exe/$csprojFolderName/bin/publish

  if [ -e $csprojFolder/Dockerfile ]
  then
    echo "It has a docker file for csproj: $csproj"
    hasDockerFile="true"
    publishFolder=$rootPath/docker-build-input/$csprojFolderName/bin/publish
  fi

  if [ "$publishOrNot" = "true" ]; then
    dotnet publish $csproj -c Release --no-build --no-restore -o $publishFolder /p:MajorVersion=$CDP_MAJOR_NUMBER_ONLY /p:MinorVersion=$CDP_MINOR_NUMBER_ONLY /p:PatchVersion=$CDP_BUILD_NUMBER /p:BuildMetadata=$CDP_DEFINITION_BUILD_COUNT

    if [ "$hasDockerFile" = "true" ]; then
      cp $csprojFolder/Dockerfile $rootPath/docker-build-input/$csprojFolderName
    fi

    echo "- - - - - [Liftr]- - - - - [https://aka.ms/liftr]- - - - - [Liftr]- - - - - [https://aka.ms/liftr]- - - - - "
    echo
  fi

}

echo "Packing ..."
SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"
echo "SrcRoot: $SrcRoot"


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
  echo "Start dotnet pack $solution"
  dotnet pack $solution -c Release --include-source --include-symbols --no-build --no-restore -o $SrcRoot/nupkgs /p:MajorVersion=$CDP_MAJOR_NUMBER_ONLY /p:MinorVersion=$CDP_MINOR_NUMBER_ONLY /p:PatchVersion=$CDP_BUILD_NUMBER /p:BuildMetadata=$CDP_DEFINITION_BUILD_COUNT
    exit_code=$?
    if [ $exit_code -ne 0 ]; then
        echo "Failed to pack"
        exit $exit_code
    fi
  echo "Finished dotnet pack $solution"
  echo "==========[Liftr]==========[https://aka.ms/liftr]==========[Liftr]==========[https://aka.ms/liftr]=========="
done

for csproj in $SrcRoot/src/*/*.csproj
do
  publishProject $csproj $SrcRoot
done

for csproj in $SrcRoot/src/*/*/*.csproj
do
  publishProject $csproj $SrcRoot
done

exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Failed to publish"
    exit $exit_code
fi

echo "Successfully finished packing solutions!"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"