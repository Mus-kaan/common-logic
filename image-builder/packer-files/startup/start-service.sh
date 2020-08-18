#!/bin/bash
maxiterations=20
logfile="/startup/vmstartup.log"
serviceName=$1
# query to see if service is actually running
until sudo service $serviceName status | grep "active (running)"
do
  # we entered the loop, which means service wasn't running, so try to start it:
  systemctl restart $serviceName
  # count this iteration
  let maxiterations-=1
  # if we've reached zero, break the loop
  if [ $maxiterations -lt 1 ]; then
    echo "`date` failed to start $serviceName" >> $logfile
    break
  fi
  # wait for 10 seconds before asking if it's running again
  sleep 10
done

if [ $maxiterations -ge 1 ]; then
    echo "`date` $serviceName started" >> $logfile
fi