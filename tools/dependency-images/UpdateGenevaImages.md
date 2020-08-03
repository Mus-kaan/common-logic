# Geneva image update
See this for reference: https://msazure.visualstudio.com/Liftr/_git/Liftr.Common/pullrequest/3212677

* Find the latest Geneva image version at here: https://genevamondocs.azurewebsites.net/collect/references/linuxcontainers.html
* Search for `[[[GENEVA_UPDATE_CHANGE_HERE]]]` in the code base. There are thres places we need to update the image tag.
* Run [this script](https://msazure.visualstudio.com/Liftr/_git/Liftr.Common?path=%2Ftools%2Fdependency-images%2FPrepareGenevaImages.sh&_a=contents&version=GBmaster) to import Geneva images to Liftr ACRs.
    * The script will first import the Geneva images from Geneva AME ACR to our liftr ACR. [The identity](https://msazure.visualstudio.com/Liftr/_git/Liftr.Common?path=%2Ftools%2Fdependency-images%2FPrepareGenevaImages.sh&version=GBmaster&line=30&lineEnd=31&lineStartColumn=1&lineEndColumn=1&lineStyle=plain) need read access over the Geneva ACR.
    * `/subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourceGroups/ev2-df-wus-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/ev2-df-shell-wus-msi` has the Geneva ACR read access. It might be the easiest to bind this MI to your dev vm and run the script. Some of our previous AKS SPN also have geneva read access.
    * Then it will pull the image locally. So, it needs docker installed.
    * After this, we will run `docker push` to push to our MS tenant ACR.