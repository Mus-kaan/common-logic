#!/bin/bash
# This script configs and starts mdsd
# Ref:https://genevamondocs.azurewebsites.net/collect/environments/linuxvm.html

set -e

currentScriptName=`basename "$0"`
currentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
logfile="/startup/vmstartup.log"
kvCertFolder="/var/lib/waagent/Microsoft.Azure.KeyVault"
msdsPort=8130

configure_gcs(){
    sudo systemctl stop mdsd

    mdsdDir="/etc/mdsd.d"
    mdmDir="/startup/mdm"
    gcsCertCertFileName="gcscert.pem"
    gcsCertKeyFileName="gcskey.pem"
    mdsdStartupScript="/etc/default/mdsd"

    kvName=$(<$currentDir/vault-name.txt)
    if [ "$kvName" = "" ]; then
        echo "Cannot find the key vault name in file vault-name.txt"  | tee -a $logfile
        exit 1 # terminate and indicate error
    fi
    echo `date`[startgeneva.sh] Certificates on disk:  | tee -a $logfile
    sudo ls $kvCertFolder  | tee -a $logfile

    sudo rm -f $mdsdDir/$gcsCertCertFileName
    sudo rm -f $mdsdDir/$gcsCertKeyFileName

    pemFileName="$kvName.GenevaClientCert"
    sudo cp $kvCertFolder/$pemFileName $mdsdDir/$gcsCertCertFileName
    sudo cp $kvCertFolder/$pemFileName $mdsdDir/$gcsCertKeyFileName
    sudo cp $kvCertFolder/$pemFileName $mdmDir/$gcsCertCertFileName
    sudo cp $kvCertFolder/$pemFileName $mdmDir/$gcsCertKeyFileName

    sudo chown syslog $mdsdDir/$gcsCertKeyFileName
    sudo chmod 644 $mdsdDir/$gcsCertCertFileName
    sudo chmod 644 $mdsdDir/$gcsCertKeyFileName
    sudo chmod 644 $mdmDir/$gcsCertCertFileName
    sudo chmod 644 $mdmDir/$gcsCertKeyFileName

    echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -" | tee -a $logfile
    echo `date`[startgeneva.sh] sudo ls -l $mdsdDir | tee -a $logfile
    sudo ls -l $mdsdDir | tee -a $logfile
    echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -" | tee -a $logfile

    # Remove old MDSD startup file
    sudo rm -f /etc/default/mdsd | tee -a $logfile
    sudo cp $currentDir/mdsd /etc/default/mdsd
}

force_restart_mdsd(){
	# let's wait 20 seconds to allow the dust to settle
	sleep 20
	maxiterations=20
	# query to see if mdsd is actually running
	until sudo service mdsd status | grep "active (running)"
	do
		echo `date`[startgeneva.sh] force_restart_mdsd restart mdsd maxiterations $maxiterations | tee -a $logfile
		# we entered the loop, which means mdsd wasn't running, so try to restart it:
		sudo service mdsd restart
		# count this iteration
		let maxiterations-=1
		# if we've reached zero, break the loop
		if [ $maxiterations -lt 1 ]; then
			break
		fi
		# wait for 10 seconds before asking if it's running again
		sleep 10
	done
}

echo `date`[startgeneva.sh] configure_gcs started | tee -a $logfile
configure_gcs

echo `date`[startgeneva.sh] force_restart_mdsd started | tee -a $logfile
force_restart_mdsd
echo `date`[startgeneva.sh] force_restart_mdsd finished | tee -a $logfile

echo `date`[startgeneva.sh] Config and restart az secpack started | tee -a $logfile
sudo azsecd config -s baseline -d P1D
sudo azsecd config -s software -d P1D
sudo azsecd config -s clamav -d P1D
sudo service azsecd restart
echo `date`[startgeneva.sh] Config and restart az secpack finished | tee -a $logfile