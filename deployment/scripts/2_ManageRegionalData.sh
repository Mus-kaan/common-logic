#!/bin/bash
# Stop on error.
set -e
CurrentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
currentScriptName=`basename "$0"`

echo "CurrentDir: $CurrentDir"
echo "currentScriptName: $currentScriptName"

if [ "$ActiveKey" = "" ]; then
    ActiveKey="Primary MongoDB Connection String"
fi

./ExecuteDeploymentRunner.sh \
--ProvisionAction="CreateOrUpdateRegionalData" \
--EnvName="$APP_ASPNETCORE_ENVIRONMENT" \
--Region="$REGION"

for script in "$CurrentDir"/2_*.sh
do
  if [[ "$script" != *"$currentScriptName"* ]]; then
    echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
    echo "Executing extension script '$script' :"
    $script
    echo "Finished Executing extension script '$script'."
    echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~"
  fi
done

echo "Successfully finished running: $currentScriptName"
echo "**********[Liftr]**********[Liftr]**********[Liftr]**********[Liftr]**********"