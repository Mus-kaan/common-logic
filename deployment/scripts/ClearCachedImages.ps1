#!/usr/bin/env pwsh

[CmdletBinding()]
param (
    $AKSName
)

function k8s-enter ([string] $node, [Parameter(Position=1, ValueFromRemainingArguments)] $node_cmd) {
    $image = "debian"
    $pod = "clear-cached-images"
    $namespace = "monitoring"
    $nsenter_cmd = @(@(
        "nsenter", "--target=1", "--mount", "--uts", "--ipc", "--net", "--pid", "--",
        $node_cmd
    ) | ForEach-Object {$_})

    $overrides = @{
        spec = @{
            nodeName = $node
            hostPID = $true;
            containers = @(@{
                securityContext = @{
                    privileged = $true;
                };
                image = $image;
                name = "nsenter";
                stdin = $true;
                stdinOnce = $true;
                tty = $true;
                command = $nsenter_cmd;
            })
        };
    } | ConvertTo-Json -Compress -Depth 10 | ConvertTo-Json

    Write-Output "Spawning $pod on $node"
    $k8s_args = @(
        "run", $pod,
        "--rm=true", "--stdin", "--tty",
        "--image=$image",
        "--overrides=$overrides",
        "--restart=Never",
        "--namespace=$namespace"
        )
    kubectl @k8s_args
}

$nodes = kubectl get nodes --cluster="$AKSName" -o="jsonpath={.items[*].metadata.name}" | Out-String
if (!$?) {
    exit 1
}
$nodes_array = $nodes.Split(' ')

$nodes_array | ForEach-Object {
    $node = $_.Trim()
    Write-Output "Node: $node"
    k8s-enter -node $node -- crictl rmi --prune
}