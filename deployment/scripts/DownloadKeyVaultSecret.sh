#!/bin/bash
# Stop on error.

for i in "$@"
do
case $i in
    --DeploymentSubscriptionId=*)
    DeploymentSubscriptionId="${i#*=}"
    shift # past argument=value
    ;;
    --VaultName=*)
    VaultName="${i#*=}"
    shift # past argument=value
    ;;
    --KeyVaultSecretName=*)
    KeyVaultSecretName="${i#*=}"
    shift # past argument=value
    ;;
    *)
    echo "Not matched option '${i#*=}' passed in."
    exit 1
    ;;
esac
done

if [ "$DeploymentSubscriptionId" = "" ]; then
echo "Please set the key vault subscrption Id using 'DeploymentSubscriptionId' ..."
exit 1 # terminate and indicate error
fi

if [ "$VaultName" = "" ]; then
echo "Please set the key vault name using 'VaultName' ..."
exit 1 # terminate and indicate error
fi

if [ "$KeyVaultSecretName" = "" ]; then
echo "Please set the key vault secret name using 'KeyVaultSecretName' ..."
exit 1 # terminate and indicate error
fi

echo "------------------------------------------------------------"
echo "az keyvault secret download --subscription $DeploymentSubscriptionId --vault-name $VaultName --name $KeyVaultSecretName --file $KeyVaultSecretName"
rm -f "$KeyVaultSecretName"
az keyvault secret download --subscription "$DeploymentSubscriptionId" --vault-name "$VaultName" --name "$KeyVaultSecretName" --file "$KeyVaultSecretName"
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "az keyvault secret download failed."
    exit $exit_code
fi
DownloadedSecret=$(<$KeyVaultSecretName)