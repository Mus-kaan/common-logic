# Helm chart for Geneva monitoring
A simple [helm](https://helm.sh/) chart for intergrating Kubernetes with Geneva services.

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

4. Create the registry secret
```shell
kubectl create secret docker-registry linuxgenevaregistry --docker-server=linuxgeneva-microsoft.azurecr.io --docker-username=<service principal id> --docker-password=<service principal key/password> --docker-email=youralias@microsoft.com
```

5. Ensure you have helm initialized correctly to work with your cluster.
```shell
kubectl config use-context <cluster-name>
kubectl config current-context
```

6. Install Geneva chart
```shell
helm upgrade aadpodidentitybinding .\chart\ --install --recreate-pods --values $BindingValuesFile --set-file gcscert.pem="<path-to-gcscert.pem>",gcskey.pem="<path-to-gcskey.pem>"
```

7. Make sure all the geneva related pods are up and running. Then go to Geneva portal check if logs show up. (It will take abot 5 mins for logs to show up)

