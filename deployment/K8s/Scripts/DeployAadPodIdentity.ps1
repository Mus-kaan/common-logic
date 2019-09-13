[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionName, 

    [Parameter(Mandatory=$true)]
    [string]$CertFile, 

    [Parameter(Mandatory=$true)]
    [string]$KubernetesClusterName,

    [Parameter(Mandatory = $true)]
    [string]$ValuesFile
)

$ErrorActionPreference = "Stop"

Login-AzureRmAccount -Subscription $SubscriptionName

# using openssl to generate cert and key file
#openssl pkcs12 -in $gcscertFile -out gcscert -nodes -clcerts -nokeys -password pass:
#openssl pkcs12 -in $gcscertFile -out gcskey -nodes -nocerts -password pass:

Write-Host "Switching kubectl context to $KubernetesClusterName"
kubectl config use-context $KubernetesClusterName

helm upgrade aadpodidentityinfra aad-pod-identity-infra\chart --install --recreate-pods --namespace default



helm upgrade aadpodidentitybinding aad-pod-identity-binding\chart --install --recreate-pods --namespace default --values aad-pod-identity-binding\values-test.yaml