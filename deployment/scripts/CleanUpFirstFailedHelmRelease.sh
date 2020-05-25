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

IsReleaseExist=$($Helm list --namespace $Namespace --filter $HelmReleaseName -q | tr '\\n' ',')
if [ "$IsReleaseExist" != "" ]
then
    echo "Release '$HelmReleaseName' exists..."
    
    echo "Checking whether First release of '$HelmReleaseName' is failed then we have to delete it to work helm install/upgrade."
    HelmReleaseStatus=$($Helm status $HelmReleaseName -o json --revision 1 --namespace $Namespace | grep -Po '"status":"\K[^"]*')
    echo "Status: '$HelmReleaseStatus'"

    if [ "$HelmReleaseStatus" = "failed" ]
    then
        echo "Deleting helm release: '$HelmReleaseName'..."
        $Helm delete $HelmReleaseName --namespace $Namespace
        echo "Helm release '$HelmReleaseName' deleted..."
    fi
else
    echo "Release '$HelmReleaseName' doesn't exist..."
fi