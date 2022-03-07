#!/bin/bash
set -e

SrcRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && pwd )"

/bin/bash $SrcRoot/build/liftr-run-linux-restore-helm.sh

/bin/bash $SrcRoot/build/liftr-run-linux-restore-crane.sh

/bin/bash $SrcRoot/build/liftr-run-linux-pack-generic-hosting-ev2-onebranch.sh out-binary-build/exe/Deployment.Runner/bin/publish out-ev2