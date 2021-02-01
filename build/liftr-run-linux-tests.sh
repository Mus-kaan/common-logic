#!/bin/bash
set -e

echo "Starting tests..."
SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"
echo "SrcRoot $SrcRoot"

rm -rf $SrcRoot/TestOutput
mkdir -p "$SrcRoot/TestOutput/CoverageResult"
mkdir -p "$SrcRoot/TestOutput/CodeCoverage"
mkdir -p "$SrcRoot/TestOutput/CoverageReport"

for solution in $SrcRoot/src/*.sln
do
  echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------"
  echo "Start dotnet test $solution"
  echo

  dotnet test $solution --logger:trx
    exit_code=$?
    if [ $exit_code -ne 0 ]; then
        echo "Failed to test"
        exit $exit_code
    fi
  echo "Finished dotnet test $solution"
  echo "==========[Liftr]==========[https://aka.ms/liftr]==========[Liftr]==========[https://aka.ms/liftr]=========="
done

echo "Successfully finished testing!"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"