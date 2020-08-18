#!/bin/bash
# This script is to be run during machine startup to address reboot issues

set -e

currentScriptName=`basename "$0"`
currentDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
logfile="/startup/vmstartup.log"

echo "----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------[https://aka.ms/liftr]----------[Liftr]----------" | tee -a $logfile
echo `date`[startup_entry.sh] currentDir: $currentDir | tee -a $logfile
echo pwd $(pwd) | tee -a $logfile
echo `date`[startup_entry.sh] startup script starts | tee -a $logfile

# if swap file doesn't exist due to VM migration, create it
if [ ! -f /mnt/swapfile ]; then
  sudo dd if=/dev/zero of=/mnt/swapfile bs=1G count=3
  sudo chown root:root /mnt/swapfile
  sudo chmod 0600 /mnt/swapfile
  sudo mkswap /mnt/swapfile
  sudo swapon /mnt/swapfile
  echo `date`[startup_entry.sh] re-created swap file | tee -a $logfile
fi

# Not having /mnt/containers will result in docker start failure.
# Create /mnt/containers folder when Linux VM is reallocated.
# /mnt/* is local disk, which is discarded when Linux VM is deallocated.
echo "=======================================================================================================================" | tee -a $logfile
echo `date`[startup_entry.sh] start docker | tee -a $logfile
sudo mkdir -p /mnt/containers
sudo ./start-service.sh docker

echo "=======================================================================================================================" | tee -a $logfile
echo `date`[startup_entry.sh] Populate container environment variables from compute tags | tee -a $logfile
sudo /bin/bash $currentDir/get-env-var-from-tags.sh

if [ $? -ne 0 ]; then
  # Report that this script failed
  echo `date`[startup_entry.sh] FAILED populate container environment variables from compute tags | tee -a $logfile
  exit 1
fi

echo "=======================================================================================================================" | tee -a $logfile
echo `date`[startup_entry.sh] Start geneva components, i.e. mdsd, AzSecPack | tee -a $logfile
sudo /bin/bash $currentDir/startgeneva.sh

for script in "$currentDir"/startup_*.sh
do
  if [[ "$script" != *"$currentScriptName"* ]]; then
    echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~" | tee -a $logfile
    echo "Executing extension script '$script' :" | tee -a $logfile
    sudo /bin/bash $script
    echo "Finished Executing extension script '$script'." | tee -a $logfile
    echo "~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~[Liftr]~~~~~~~~~~[https://aka.ms/liftr]~~~~~~~~~~" | tee -a $logfile
  fi
done

sudo docker ps | tee -a $logfile
echo `date`[startup_entry.sh] startup script ends | tee -a $logfile