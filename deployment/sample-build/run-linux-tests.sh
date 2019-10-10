#!/bin/bash

set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Starting tests..."

dotnet test $DIR/../src/Liftr.Nginx.sln --logger:trx
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Failed to test"
    exit $exit_code
fi
echo "Finished tests successfully"
