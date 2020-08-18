There are some existing scripts we will leverage from the common repo's nuget package. Those will be copied to this folder during build time.

## [startup_entry.sh](https://msazure.visualstudio.com/Liftr/_git/Liftr.Common?path=%2Fimage-builder%2Fpacker-files%2Fstartup%2Fstartup_entry.sh)
This is the entry script after each VM start up. It will do some initializations like start docker, mdsd, AzSecPack. It is providing an extensibility point of calling all the scripts in this folder with pattern `startup_*.sh`.

## [startgeneva.sh](https://msazure.visualstudio.com/Liftr/_git/Liftr.Common?path=%2Fimage-builder%2Fpacker-files%2Fstartup%2Fstartgeneva.sh)
Start mdsd and AzSecPack.

## [get-env-var-from-tags.sh](https://msazure.visualstudio.com/Liftr/_git/Liftr.Common?path=%2Fimage-builder%2Fpacker-files%2Fstartup%2Fget-env-var-from-tags.sh)
Populate all the environment variables from VM tags. The result will be written to `vm-global.env`.

## [mdsd-template.sh](https://msazure.visualstudio.com/Liftr/_git/Liftr.Common?path=%2Fimage-builder%2Fpacker-files%2Fstartup%2Fmdsd-template.sh)
mdsd configuration template.

## [start-service.sh](https://msazure.visualstudio.com/Liftr/_git/Liftr.Common?path=%2Fimage-builder%2Fpacker-files%2Fstartup%2Fstart-service.sh)
Utility script for start service.