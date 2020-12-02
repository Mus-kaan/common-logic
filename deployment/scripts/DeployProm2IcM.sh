#!/bin/bash
# Stop on error.
set -e

Helm="./helm"
helmReleaseName="icm-rel"
namespace="prometheus"

echo "************************************************************"
echo "Start helm upgrade prom2icm chart ..."
echo "************************************************************"

if [ ! -f bin/icm-connector-id.txt ]; then
    echo "Skip deploying prom2icm due to cannot find 'bin/icm-connector-id.txt'"
    exit 0
fi

if [ ! -f bin/icm-email.txt ]; then
    echo "Skip deploying prom2icm due to cannot find 'bin/icm-email.txt'"
    exit 0
fi

echo "Read IcmConnectorId from file 'bin/icm-connector-id.txt'."
IcmConnectorId=$(<bin/icm-connector-id.txt)
if [ "$IcmConnectorId" = "" ]; then
    exit 1 # terminate and indicate error
fi
echo "IcmConnectorId: $IcmConnectorId"

echo "Read IcmEmail from file 'bin/icm-email.txt'."
IcmEmail=$(<bin/icm-email.txt)
if [ "$IcmEmail" = "" ]; then
    exit 1 # terminate and indicate error
fi
echo "IcmEmail: $IcmEmail"

echo "Read KeyVaultEndpoint from file 'bin/aks-kv.txt'."
KeyVaultEndpoint=$(<bin/aks-kv.txt)
if [ "$KeyVaultEndpoint" = "" ]; then
    exit 1 # terminate and indicate error
fi
echo "KeyVaultEndpoint: $KeyVaultEndpoint"

echo "helm upgrade $helmReleaseName ..."

$Helm upgrade $helmReleaseName --install --wait --timeout 10m \
--create-namespace \
--set icm.certificateKeyVault="$KeyVaultEndpoint" \
--set icm.connectorId="$IcmConnectorId" \
--set icm.notificatEmail="$IcmEmail" \
--namespace "$namespace" prom2icm-*.tgz

echo "------------------------------------------------------------"
echo "Finished helm upgrade prom2icm chart"
echo "------------------------------------------------------------"