#!/bin/bash
set -e

execute_command_until_success(){
    if [ ! -z "$2" ]; then
        counter=$2
    else
        counter=120  # 2 minutes by default
    fi

    echo "[liftr-image-builder] Execute command: '$1'"

    until
        eval $1
    do
        counter=$[$counter-10]
        if [ $counter -le 0 ]; then
            echo "[liftr-image-builder] Max retry reached for command \"$1\", exiting"
            exit 1
        fi
        sleep 10
    done
}

echo '[liftr-image-builder] install unzip'
execute_command_until_success 'sudo apt-get install -y unzip'