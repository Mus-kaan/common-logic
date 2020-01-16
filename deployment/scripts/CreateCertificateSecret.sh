#!/bin/bash
# Stop on error.
set -e

for i in "$@"
do
case $i in
    --DeploymentSubscriptionId=*)
    DeploymentSubscriptionId="${i#*=}"
    shift # past argument=value
    ;; # past argument=value
    --VaultName=*)
    VaultName="${i#*=}"
    shift # past argument=value
    ;;
    --KeyVaultSecretName=*)
    KeyVaultSecretName="${i#*=}"
    shift # past argument=value
    ;;
    --tlsSecretName=*)
    tlsSecretName="${i#*=}"
    shift # past argument=value
    ;;
    --caSecretName=*)
    caSecretName="${i#*=}"
    shift # past argument=value
    ;;
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

if [ -z ${DeploymentSubscriptionId+x} ]; then
    echo "DeploymentSubscriptionId is blank."
    exit 1
fi

if [ -z ${VaultName+x} ]; then
    echo "VaultName is blank."
    exit 1
fi

if [ -z ${KeyVaultSecretName+x} ]; then
    echo "KeyVaultSecretName is blank."
    exit 1
fi

if [ -z ${tlsSecretName+x} ]; then
    echo "tlsSecretName is blank."
    exit 1
fi

if [ -z ${caSecretName+x} ]; then
    echo "caSecretName is blank."
    exit 1
fi

if [ -z ${Namespace+x} ]; then
    echo "Namespace is blank."
    exit 1
fi


echo "az keyvault secret download --subscription "$DeploymentSubscriptionId" --vault-name "$VaultName" --name $KeyVaultSecretName --file cert-pfx-encoded"
rm -f cert-pfx-encoded
az keyvault secret download --subscription "$DeploymentSubscriptionId" --vault-name "$VaultName" --name "$KeyVaultSecretName" --file cert-pfx-encoded
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "az keyvault secret download failed."
    exit $exit_code
fi

echo "(cat cert-pfx-encoded | base64 --decode) > cert-pfx.pfx"
rm -f cert-pfx.pfx
(cat cert-pfx-encoded | base64 --decode) > "$KeyVaultSecretName.pfx"
rm -f cert-pfx-encoded

openssl pkcs12 -in "$KeyVaultSecretName.pfx" -out "$KeyVaultSecretName.key" -nodes -nocerts -password pass:
openssl pkcs12 -in "$KeyVaultSecretName.pfx" -out "$KeyVaultSecretName.cer" -nodes -clcerts -nokeys -password pass:
openssl pkcs12 -in "$KeyVaultSecretName.pfx" -out "$KeyVaultSecretName.full.cer" -nodes -nokeys -password pass:
openssl pkcs12 -in "$KeyVaultSecretName.pfx" -out "$KeyVaultSecretName.cacerts.cer" -nodes -cacerts -chain -nokeys -password pass:

set +e
kubectl delete secret -n "$Namespace" "$tlsSecretName"
kubectl delete secret -n "$Namespace" "$caSecretName"
set -e

# a secret to be used for TLS termination
echo "kubectl create secret tls -n $Namespace $tlsSecretName --key $KeyVaultSecretName.key --cert $KeyVaultSecretName.cer"
kubectl create secret tls -n "$Namespace" "$tlsSecretName" --key "$KeyVaultSecretName.key" --cert "$KeyVaultSecretName.cer"

# a secret to be used for client authenticating using the same CA
echo "kubectl create secret generic -n $Namespace $caSecretName --from-file=ca.crt=$KeyVaultSecretName.cacerts.cer"
kubectl create secret generic -n "$Namespace" "$caSecretName" --from-file=ca.crt="$KeyVaultSecretName.cacerts.cer"
