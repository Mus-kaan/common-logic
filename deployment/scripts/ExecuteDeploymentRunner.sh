#!/bin/bash

# Stop on error.
set -e

for i in "$@"
do
case $i in
    --ProvisionAction=*)
    ProvisionAction="${i#*=}"
    shift # past argument=value
    ;;
    --ConfigFilePath=*)
    ConfigFilePath="${i#*=}"
    shift # past argument=value
    ;;
    --EnvName=*)
    EnvName="${i#*=}"
    shift # past argument=value
    ;;
    --Region=*)
    Region="${i#*=}"
    shift # past argument=value
    ;;
    --ActiveKey=*)
    ActiveKey="${i#*=}"
    shift # past argument=value
    ;;
    --AKSSvcLabel=*)
    AKSSvcLabel="${i#*=}"
    shift # past argument=value
    ;;
    *)
    echo "Not matched option '${i#*=}' passed in."
    exit 1
    ;;
esac
done

if [ -z ${ProvisionAction+x} ]; then
    echo "ProvisionAction is blank."
    exit 1
fi

if [ -z ${ConfigFilePath+x} ]; then
    echo "ConfigFilePath is blank. Use 'hosting-options.json'."
    ConfigFilePath="hosting-options.json"
fi

if [ -z ${EnvName+x} ]; then
    echo "EnvName is blank."
    exit 1
fi

if [ -z ${Region+x} ]; then
    echo "Region is blank."
    exit 1
fi

if [ -z ${ActiveKey+x} ]; then
    ActiveKey="Primary MongoDB Connection String"
fi

if [ "$ProvisionAction" = "UpdateAKSPublicIpInTrafficManager" ]; then
    if [ -z ${AKSSvcLabel+x} ]; then
        echo "AKSSvcLabel is blank."
        exit 1
    fi
fi

echo "Use configuration file from path: $ConfigFilePath"

cd bin
echo "----------------------------------------------------------------------------------------------"
echo "Configration file content: "
echo
cat $ConfigFilePath
echo
echo "----------------------------------------------------------------------------------------------"

if [ "$AKSSvcLabel" = "" ]; then
    dotnet Deployment.Runner.dll \
    -a "$ProvisionAction" \
    -f "$ConfigFilePath" \
    -e "$EnvName" \
    -r "$Region" \
    --activeKey "$ActiveKey"
else
    dotnet Deployment.Runner.dll \
    -a "$ProvisionAction" \
    -f "$ConfigFilePath" \
    -e "$EnvName" \
    -r "$Region" \
    -l "$AKSSvcLabel" \
    --activeKey "$ActiveKey"
fi

exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "Failed to run deployment runner."
    exit $exit_code
fi

echo "----------------------------------------------------------------------------------------------"
echo "Finished running the deployment runner."
echo "----------------------------------------------------------------------------------------------"