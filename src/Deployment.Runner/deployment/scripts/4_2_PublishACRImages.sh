#!/bin/bash

CraneFile="crane"
#If crane is in EV2 tar, this came from a onebranch build
if [ -f "$CraneFile" ]; then
    ./PublishTarArtifactsImages.sh
else
    ./PublishCDPxACRImages.sh
fi