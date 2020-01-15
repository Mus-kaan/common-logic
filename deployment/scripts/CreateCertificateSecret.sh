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
    --k8sSecretName=*)
    k8sSecretName="${i#*=}"
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

if [ -z ${k8sSecretName+x} ]; then
    echo "k8sSecretName is blank."
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
(cat cert-pfx-encoded | base64 --decode) > cert.pfx

openssl pkcs12 -in cert.pfx -out cert.key -nodes -nocerts -password pass:
openssl pkcs12 -in cert.pfx -out cert.cer -nodes -clcerts -nokeys -password pass:
openssl pkcs12 -in cert.pfx -out cacerts.cer -nodes -cacerts -chain -nokeys -password pass:

# openssl pkcs12 -in cert.pfx -nocerts -nodes -password pass: | sed -ne '/-BEGIN PRIVATE KEY-/,/-END PRIVATE KEY-/p' > cert.key
# openssl pkcs12 -in cert.pfx -clcerts -nokeys -password pass: | sed -ne '/-BEGIN CERTIFICATE-/,/-END CERTIFICATE-/p' > cert.cer
# openssl pkcs12 -in cert.pfx -cacerts -nokeys -chain -password pass: | sed -ne '/-BEGIN CERTIFICATE-/,/-END CERTIFICATE-/p' > cacerts.cer

set +e
kubectl delete secret -n "$Namespace" "$k8sSecretName"
kubectl delete secret -n "$Namespace" thanos-ca-secret
set -e

# a secret to be used for TLS termination
kubectl create secret tls -n "$Namespace" "$k8sSecretName" --key ./cert.key --cert ./cert.cer

# a secret to be used for client authenticating using the same CA
kubectl create secret generic -n "$Namespace" thanos-ca-secret --from-file=ca.crt=./cacerts.cer

# rm -f cert.pfx
rm -f cert.key
rm -f cert.cer
rm -f cacerts.cer