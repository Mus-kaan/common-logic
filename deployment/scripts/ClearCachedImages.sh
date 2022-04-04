#!/bin/bash

k8s-enter(){
    args=("$@")
    argsCount=${#args[@]}
    node=${args[0]}
    unset args[0]
    node_cmd=${args[@]}
    image="debian"
    pod="clear-cached-images"
    namespace="monitoring"
    echo "spawning $pod on $node"
    overrides=$(<'ClearCachedImagesOverrides.json')
    overrides=${overrides//insert_node_name/$node}
    kubectl run "$pod" --rm="true" --stdin --tty --image="$image" --overrides="$overrides" --restart="Never" --namespace="$namespace"
}

AKSName=$1
echo "AKS Name: $AKSName"
nodes=$(kubectl get nodes --cluster="$AKSName" -o="jsonpath={.items[*].metadata.name}")

nodes_array=($nodes)
for nodeName in ${nodes_array[@]}
do
    echo "Node: $nodeName"
    k8s-enter $nodeName crictl rmi --prune
done