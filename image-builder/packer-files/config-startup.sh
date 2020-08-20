#!/bin/bash
# This script will add a startup cron job so that when machine restarts we can execute some desired tasks

set -e

dos2unix(){
  sudo tr -d '\r' < "$1" > t
  sudo mv -f t "$1"
}

startupDir="/startup/"
startupscriptfile="startup_entry.sh"

# configure_startup ensures we have a script that runs after every reboot
configure_startup(){
	sudo rm -rf $startupDir
	if [ ! -d $startupDir ]; then
		sudo mkdir $startupDir
	fi
	startupsh=$startupDir$startupscriptfile

	# create cron job for startup script:
	sudo crontab <<EOF
@reboot $startupsh &
EOF

	sudo cp -r ./startup/* $startupDir
	sudo touch /startup/vmstartup.log
	sudo chmod u=rwx,g=rwx,o=rx /startup
	sudo chmod u=rwx,g=rwx,o=rx /startup/vmstartup.log

  	for script in $startupDir*.sh
	do
		echo "dos2unix $script"
		dos2unix $script
		sudo chmod u=rwx,g=rwx,o=rx $script
	done
}

echo "[liftr | configstartup.sh] configure_startup started"
configure_startup
echo "[liftr | configstartup.sh] configure_startup finished"

