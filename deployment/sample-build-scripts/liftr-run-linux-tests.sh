#!/bin/bash
set -e

echo "Starting tests..."
SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"
echo "SrcRoot $SrcRoot"

for solution in $SrcRoot/src/*.sln
do
  echo "----------[Liftr]----------[Liftr]----------[Liftr]----------[Liftr]----------"
  echo "Start dotnet test $solution"
  echo

  dotnet test $solution --logger:trx
    exit_code=$?
    if [ $exit_code -ne 0 ]; then
        echo "Failed to test"
        exit $exit_code
    fi
  echo "Finished dotnet test $solution"
  echo "==========[Liftr]==========[Liftr]==========[Liftr]==========[Liftr]=========="
done

echo "Successfully testing!"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"