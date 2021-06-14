#!/bin/bash
set -e

echo "Starting tests..."
SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"
echo "SrcRoot $SrcRoot"

dotnet test $SrcRoot/src/Liftr.Fluent.Tests/Liftr.Fluent.Tests.csproj --no-build --filter RegionCategory=PublicWestUS2 --logger:trx -v:diag
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Failed to test"
    exit $exit_code
fi

echo "Successfully finished testing!"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"