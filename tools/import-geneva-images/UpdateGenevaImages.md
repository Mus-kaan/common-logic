# Geneva image update
See this for reference: https://msazure.visualstudio.com/Liftr/_git/Liftr.Common/pullrequest/3212677

* Find the latest Geneva image version at here: https://genevamondocs.azurewebsites.net/collect/references/linuxcontainers.html
* Search for `[[[GENEVA_UPDATE_CHANGE_HERE]]]` in the code base. There are thres places we need to update the image tag.
* Run the [Import Geneva images](https://msazure.visualstudio.com/Liftr/_release?_a=releases&view=mine&definitionId=129) release to do the actual import Geneva image work. This will also be automatically executed after merge to master.