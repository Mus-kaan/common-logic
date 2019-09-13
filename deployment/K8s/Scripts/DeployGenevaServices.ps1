# Scrips will be rewrite to bash script when we start writing Ev2 shell extension. For now It's just for local test.
[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionName, 

    [Parameter(Mandatory=$true)]
    [string]$AadPodIdentityChartFolder, 

    [Parameter(Mandatory=$true)]
    [string]$KubernetesClusterName,

    [Parameter(Mandatory = $true)]
    [string]$BindingValuesFile
)

$ErrorActionPreference = "Stop"

Login-AzureRmAccount -Subscription $SubscriptionName

Write-Host "Switching kubectl context to $KubernetesClusterName"
kubectl config use-context $KubernetesClusterName

helm upgrade aadpodidentityinfra aad-pod-identity-infra\chart --install --recreate-pods --namespace default

helm upgrade aadpodidentityinfra "$AadPodIdentityChartFolder\aad-pod-identity-infra\chart" --install --recreate-pods --namespace default

# Naively sleep for 2 mins waiting for infrastructure
Start-Sleep -Seconds 120

helm upgrade aadpodidentitybinding "$AadPodIdentityChartFolder\aad-pod-identity-binding\chart" --install --recreate-pods --namespace default --values $BindingValuesFile