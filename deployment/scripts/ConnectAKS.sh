#!/bin/bash
# Stop on error.
set -e

if [ "$AKSRGName" = "" ]; then
AKSRGName=$(<bin/aks-rg.txt)
    if [ "$AKSRGName" = "" ]; then
        echo "Please set the name of the AKS cluster Resource Group using variable 'AKSRGName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "AKSRGName: $AKSRGName"

if [ "$AKSName" = "" ]; then
AKSName=$(<bin/aks-name.txt)
    if [ "$AKSName" = "" ]; then
        echo "Please set the name of the AKS cluster using variable 'AKSName' ..."
        exit 1 # terminate and indicate error
    fi
fi
echo "AKSName: $AKSName"

# Connect AKS cluster - get credential for local admin account
# If AAD is not enabled for AKS cluster, with/without `--admin` won't make difference; if enabled,
# the local admin account credential will bypass AAD authentication to unblock us here
echo "az aks get-credentials -g $AKSRGName -n $AKSName --admin"
az aks get-credentials -g "$AKSRGName" -n "$AKSName" --admin
