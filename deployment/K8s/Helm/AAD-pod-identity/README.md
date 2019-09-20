# Helm chart for Azure Active Directory Pod Identity
A simple [helm](https://helm.sh/) chart for setting up the components needed to use [Azure Active Directory Pod Identity](https://github.com/Azure/aad-pod-identity) in Kubernetes.

## Chart resources
This helm chart will deploy the following resources:
* AzureIdentity `CustomResourceDefinition`
* AzureIdentityBinding `CustomResourceDefinition`
* AzureAssignedIdentity `CustomResourceDefinition`
* AzureIdentity instance (optional)
* AzureIdentityBinding instance (optional)
* Managed Identity Controller (MIC) `Deployment`
* Node Managed Identity (NMI) `DaemonSet`

## Getting Started
The following steps will help you create a new Azure identity ([Managed Service Identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview) or [Service Principal](https://docs.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals)) and assign it to pods running in your Kubernetes cluster.

### Prerequisites
1. Create AKS cluster
```shell
az group create --name <resource-group-name> --location <location>
```

```shell
az aks create --resource-group <resource-group-name> --name <aks-cluster-name> --node-count 1 --generate-ssh-keys --service-principal <service-principal-id> --client-secret <service-principal-key/password>
```
2. Configure kubectl to connect to your AKS cluster
```shell
az aks get-credentials --resource-group <resource-group-name> --name <aks-cluster-name>
```

3. Initialize Helm(version 2.*)
```shell
helm init
kubectl create serviceaccount --namespace kube-system tiller
kubectl create clusterrolebinding tiller-cluster-rule --clusterrole=cluster-admin --serviceaccount=kube-system:tiller
```

### Steps

4. Create an [Azure Identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview) using the Azure CLI:
> __NOTE:__ It's simpler to use the same resource group as your Kubernetes nodes are deployed in. For AKS this is the MC_* resource group. If you can't use the same resource group, you'll need to grant the Kubernetes cluster's service principal the "Managed Identity Operator" role.
```shell
az identity create -g <resource-group-name> -n <name> -o json
```

5. Assign your newly created identity the role of _Reader_ for the target resource group:
```shell
az role assignment create --role Reader --assignee <principal-id> --scope /subscriptions/<subscription-id>/resourcegroups/<resource-group>
```

6. Ensure you have helm initialized correctly to work with your cluster.
```shell
kubectl config use-context <cluster-name>
kubectl config current-context
```

7. Navigate to `.\aad-pod-identity-infra` folder. Install the helm chart into your Kubernetes cluster.
```shell
helm upgrade aadpodidentityinfra --install --namespace default .\chart\
```

If it complians about issue like `configmaps is forbidden: User "system:serviceaccount:kube-system:default" cannot list resource "configmaps" in API group "" in the namespace "kube-system"`. Run the following command:
```shell
helm init --service-account tiller --upgrade
```

8. Navigate to folder '.\aad-pod-identity-binding\' update `values-base.yaml`. Replace fields with the info from the azure identity created in step 4. Then install the chart.
```shell
helm upgrade aadpodidentitybinding --install --values values-base.yaml --namespace default .\chart\
```

9. Deploy your application to Kubernetes. Make sure the selector you defined in your `AzureIdentityBinding` matches the `aadpodidbinding` label on the deployment.

10. Make sure NMI and MIC are running correctly buy check logs with `kubectl logs *`. And also make sure your application is up and running.

