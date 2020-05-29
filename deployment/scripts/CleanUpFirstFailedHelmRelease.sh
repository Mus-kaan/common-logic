#!/bin/bash
# Stop on error.
set -e

for i in "$@"
do
case $i in
    --HelmReleaseName=*)
    HelmReleaseName="${i#*=}"
    shift # past argument=value
    ;; # past argument=value
    --Namespace=*)
    Namespace="${i#*=}"
    shift # past argument=value
    ;; # past argument=value
    *)
    echo "Not matched option '${i#*=}' passed in."
    exit 1
    ;;
esac
done

if [ -z ${HelmReleaseName+x} ]; then
    echo "HelmReleaseName is blank."
    exit 1
fi

if [ -z ${Namespace+x} ]; then
    echo "Namespace is blank."
    exit 1
fi

Helm="./helm"
echo "HelmReleaseName: $HelmReleaseName"
echo "Namespace: $Namespace"

echo "Checking status of First Helm release '$HelmReleaseName'..."

if $Helm status $HelmReleaseName -o json --revision 1 --namespace $Namespace | grep -Po '"status":"\K[^"]*'; then
    HelmReleaseStatus=$($Helm status $HelmReleaseName -o json --revision 1 --namespace $Namespace | grep -Po '"status":"\K[^"]*')
    echo "Helm release '$HelmReleaseName' | Status '$HelmReleaseStatus' | Revision 1..."
    if [ "$HelmReleaseStatus" = "failed" ]
    then
        echo "Deleting helm release: '$HelmReleaseName'..."
        $Helm delete $HelmReleaseName --namespace $Namespace
        echo "Helm release '$HelmReleaseName' deleted..."
    fi
else
    echo "First Helm Release '$HelmReleaseName' doesn't exist... Running Helm install/upgrade now."    
fi