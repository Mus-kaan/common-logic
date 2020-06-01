#!/bin/bash
# Stop on error.

echo "Checking jq ..."
set +e
jqInstalled="$(jq --version)"
set -e
if [ "$jqInstalled" = "" ]; then
    echo "Installing jq ..."
    apt-get update
    apt-get install jq -y
else
    echo "jq is already installed. Skip."
fi
