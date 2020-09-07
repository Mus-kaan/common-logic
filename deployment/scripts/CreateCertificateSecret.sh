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


rm -f "$KeyVaultSecretName.txt"
echo "az keyvault secret download --subscription $DeploymentSubscriptionId --vault-name $VaultName --name $KeyVaultSecretName --file $KeyVaultSecretName.txt"
az keyvault secret download --subscription "$DeploymentSubscriptionId" --vault-name "$VaultName" --name "$KeyVaultSecretName" --file "$KeyVaultSecretName.txt"
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "az keyvault secret '$KeyVaultSecretName' download failed."
    exit $exit_code
fi

rm -f "$KeyVaultSecretName.pfx"
echo "(cat $KeyVaultSecretName.txt | base64 --decode) > $KeyVaultSecretName.pfx"
(cat "$KeyVaultSecretName.txt" | base64 --decode) > "$KeyVaultSecretName.pfx"

openssl pkcs12 -in "$KeyVaultSecretName.pfx" -out "$KeyVaultSecretName.key" -nodes -nocerts -password pass:
openssl pkcs12 -in "$KeyVaultSecretName.pfx" -out "$KeyVaultSecretName.reverse.cer" -nodes -clcerts -nokeys -password pass:
openssl pkcs12 -in "$KeyVaultSecretName.pfx" -out "$KeyVaultSecretName.full.cer" -nodes -nokeys -password pass:
openssl pkcs12 -in "$KeyVaultSecretName.pfx" -out "$KeyVaultSecretName.cacerts.cer" -nodes -cacerts -chain -nokeys -password pass:

# The "$KeyVaultSecretName.reverse.cer" file has the cert chain in it. However, since it is from pfx file, the first one is its own cert.
# Nginx requires the first cert to be the CA's cert and self cert to be in the last. We need to reverse the cert below.
tlsCert="$KeyVaultSecretName.temp.cer"
pattern="$KeyVaultSecretName-splited-temp-cert-"
chainedCert="$KeyVaultSecretName.cer"

rm -f "$tlsCert"
rm -f *splited-temp-cert*
rm -f "$chainedCert"

openssl pkcs12 -in "$KeyVaultSecretName.pfx" -nokeys -password pass: | sed -ne '/-BEGIN CERTIFICATE-/,/-END CERTIFICATE-/p' > "$tlsCert"

# Split individual cert to a separate file.
csplit -f $pattern -z $tlsCert '/-----BEGIN CERTIFICATE-----/' '{*}'
numCerts="$( ls -1q ${pattern}* | wc -l )"
echo "The cert chain has ${numCerts} certs."

# Merge the cert chain (assume the cert chain has no more than 10 certs)
for ((i=numCerts-1; i>=0; i--))
do
    cat "${pattern}0${i}" >>$chainedCert
done

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

rm -f "$KeyVaultSecretName.txt"
rm -f "$KeyVaultSecretName.pfx"