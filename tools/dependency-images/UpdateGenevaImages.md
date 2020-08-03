# Geneva image update
See this for reference: https://msazure.visualstudio.com/Liftr/_git/Liftr.Common/pullrequest/3212677

* Find the latest Geneva image version at here: https://genevamondocs.azurewebsites.net/collect/references/linuxcontainers.html
* Search for `[[[GENEVA_UPDATE_CHANGE_HERE]]]` in the code base. There are thres places we need to update the image tag.
* Run [this script](https://msazure.visualstudio.com/Liftr/_git/Liftr.Common?path=%2Ftools%2Fdependency-images%2FPrepareGenevaImages.sh&_a=contents&version=GBmaster) to import Geneva images to Liftr ACRs.