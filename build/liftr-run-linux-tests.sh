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

  dotnet test $solution --logger:trx --collect:"XPlat Code Coverage" /p:CollectCoverage=true /p:CoverletOutputFormat="json%2cCobertura" /p:CoverletOutput="$SrcRoot/TestOutput/CodeCoverage/" /p:MergeWith="$SrcRoot/TestOutput/CodeCoverage/coverage.json" --results-directory="$SrcRoot/TestOutput/CoverageResult/"
    exit_code=$?
    if [ $exit_code -ne 0 ]; then
        echo "Failed to test"
        exit $exit_code
    fi
  echo "Finished dotnet test $solution"
  echo "==========[Liftr]==========[https://aka.ms/liftr]==========[Liftr]==========[https://aka.ms/liftr]=========="
done

echo "-------- Generating Code Coverage report ------------------------"
$SrcRoot/buildtools/reportgenerator "-reports:$SrcRoot/TestOutput/CodeCoverage/coverage.cobertura.xml" "-targetdir:$SrcRoot/TestOutput/CoverageReport" -reporttypes:HtmlInline_AzurePipelines                 # Generates report
EX=$?

if [ "$EX" -ne 0 ]; then
    echo -e "${RED}Error while generating code coverage report ${NC}"
    exit $EX
fi

echo "===============Code coverage Report Generated=============="

echo "Successfully finished testing!"
echo "**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********"