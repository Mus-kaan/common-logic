#!/bin/bash
set -e

SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"
/bin/bash $SrcRoot/build/liftr-run-linux-pack-generic-hosting-ev2.sh src/Deployment.Runner.GlobalService/bin/publish out-ev2-global