#!/bin/bash
set -e

currentScriptName=`basename "$0"`
currentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

logfile="/startup/vmstartup.log"
vmEnvFile="$currentDir/vm-global.env"
mdsdConfigFile="$currentDir/mdsd"
mdsdConfigTemplate="$currentDir/mdsd-template.sh"
mdmConfigFile="$currentDir/mdm/mdm.env"
mdmConfigTemplate="$currentDir/mdm-template.env"
vaultNameFile="$currentDir/vault-name.txt"

echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------" | tee -a $logfile
echo [`date`] currentDir: $currentDir | tee -a $logfile

sudo mkdir -p $currentDir/mdm

sudo rm -rf $vmEnvFile
sudo rm -rf $mdsdConfigFile
sudo rm -rf $mdmConfigFile
sudo rm -rf $vaultNameFile

sudo touch $vmEnvFile
sudo touch $vaultNameFile
sudo cp $mdsdConfigTemplate $mdsdConfigFile
sudo cp $mdmConfigTemplate $mdmConfigFile

chmod u=rw,g=rw,o=r $vmEnvFile
chmod u=rw,g=rw,o=r $vaultNameFile
chmod u=rw,g=rw,o=r $mdsdConfigFile
chmod u=rw,g=rw,o=r $mdmConfigFile
chmod u=rw,g=rw,o=r $logfile
chmod u=rwx,g=rwx,o=rx $currentDir

echo [`date`] Starting GET tags from VM instance metadata service | tee -a $logfile
# https://docs.microsoft.com/en-us/azure/virtual-machines/windows/instance-metadata-service#vm-tags
tagList=$(curl -H "Metadata:true" --silent "http://169.254.169.254/metadata/instance/compute/tagsList?api-version=2019-06-04")
# echo [`date`] tagList: $tagList  | tee -a $logfile

tagObjectList=$(echo "$tagList" | jq -c '.[]')

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -" | tee -a $logfile
echo [`date`] Generating docker compose Env file vm-global.env | tee -a $logfile
for tagKvp in ${tagObjectList}
do
	# echo "Parsing tagKvp: $tagKvp"
    tagName=$(echo "$tagKvp" | jq -r '.name')
    tagValue=$(echo "$tagKvp" | jq -r '.value')
    if  [[ $tagName =~ ^ENV_* ]] ;
    then
        envName=$(echo ${tagName:4})
        envPair=$(echo "$envName=$tagValue")
        echo "$envPair" >> $vmEnvFile

        if  [[ $tagName =~ ^ENV_MONITORING_GCS_REGION ]] ;
        then
            MONITORING_GCS_REGION=$tagValue
        fi

        if  [[ $tagName =~ ^ENV_MONITORING_CONFIG_VERSION ]] ;
        then
            MONITORING_CONFIG_VERSION=$tagValue
        fi

        if  [[ $tagName =~ ^ENV_MONITORING_GCS_ACCOUNT ]] ;
        then
            MONITORING_GCS_ACCOUNT=$tagValue
        fi

        if  [[ $tagName =~ ^ENV_MONITORING_GCS_ENVIRONMENT ]] ;
        then
            MONITORING_GCS_ENVIRONMENT=$tagValue
        fi

        if  [[ $tagName =~ ^ENV_MONITORING_GCS_NAMESPACE ]] ;
        then
            MONITORING_GCS_NAMESPACE=$tagValue
        fi

        if  [[ $tagName =~ ^ENV_VMSS_NAME ]] ;
        then
            MONITORING_ROLE=$tagValue
        fi

        if  [[ $tagName =~ ^ENV_IMG_NAME ]] ;
        then
            MONITORING_TENANT=$tagValue
        fi

        if  [[ $tagName =~ ^ENV_VaultName ]] ;
        then
            echo "$tagValue" >> $vaultNameFile
        fi

        if  [[ $tagName =~ ^ENV_MDM_ACCOUNT ]] ;
        then
            MDM_ACCOUNT=$tagValue
        fi

        if  [[ $tagName =~ ^ENV_MDM_NAMESPACE ]] ;
        then
            MDM_NAMESPACE=$tagValue
        fi

        if  [[ $tagName =~ ^ENV_MDM_ENDPOINT ]] ;
        then
            MDM_ENDPOINT=$tagValue
        fi
    fi
done

echo [`date`] Generated docker compose Env file vm-global.env: | tee -a $logfile
cat $vmEnvFile | tee -a $logfile

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -" | tee -a $logfile
echo [`date`] Generating mdsd config file ... | tee -a $logfile
MONITORING_ROLE_INSTANCE=$(hostname)
echo MONITORING_GCS_ENVIRONMENT:    $MONITORING_GCS_ENVIRONMENT | tee -a $logfile
echo MONITORING_GCS_ACCOUNT:        $MONITORING_GCS_ACCOUNT | tee -a $logfile
echo MONITORING_GCS_NAMESPACE:      $MONITORING_GCS_NAMESPACE | tee -a $logfile
echo MONITORING_GCS_REGION:         $MONITORING_GCS_REGION | tee -a $logfile
echo MONITORING_CONFIG_VERSION:     $MONITORING_CONFIG_VERSION | tee -a $logfile

echo MONITORING_TENANT:             $MONITORING_TENANT | tee -a $logfile
echo MONITORING_ROLE:               $MONITORING_ROLE | tee -a $logfile
echo MONITORING_ROLE_INSTANCE:      $MONITORING_ROLE_INSTANCE | tee -a $logfile

echo MDM_ACCOUNT:                   $MDM_ACCOUNT | tee -a $logfile
echo MDM_NAMESPACE:                 $MDM_NAMESPACE | tee -a $logfile
echo MDM_ENDPOINT:                  $MDM_ENDPOINT | tee -a $logfile


sed -i "s|PLACEHOLDER_MONITORING_GCS_ENVIRONMENT|$MONITORING_GCS_ENVIRONMENT|g" $mdsdConfigFile
sed -i "s|PLACEHOLDER_MONITORING_GCS_ACCOUNT|$MONITORING_GCS_ACCOUNT|g" $mdsdConfigFile
sed -i "s|PLACEHOLDER_MONITORING_GCS_NAMESPACE|$MONITORING_GCS_NAMESPACE|g" $mdsdConfigFile
sed -i "s|PLACEHOLDER_MONITORING_GCS_REGION|$MONITORING_GCS_REGION|g" $mdsdConfigFile
sed -i "s|PLACEHOLDER_MONITORING_CONFIG_VERSION|$MONITORING_CONFIG_VERSION|g" $mdsdConfigFile

sed -i "s|PLACEHOLDER_MONITORING_TENANT|$MONITORING_TENANT|g" $mdsdConfigFile
sed -i "s|PLACEHOLDER_MONITORING_ROLE_INSTANCE|$MONITORING_ROLE_INSTANCE|g" $mdsdConfigFile
sed -i "s|PLACEHOLDER_MONITORING_ROLE|$MONITORING_ROLE|g" $mdsdConfigFile

sed -i "s|PLACEHOLDER_MDM_ACCOUNT|$MDM_ACCOUNT|g" $mdmConfigFile