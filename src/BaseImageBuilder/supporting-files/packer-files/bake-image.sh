#!/bin/bash
set -e

# It is highly recommend to have some special characters to mark the start of the script,
# e.g. '[bake-image.sh | start]'. Then it will be easy to search in the AIB packer logs.
# Details: https://aka.ms/liftr/aib-tsg
echo "------------------------------------------------------------------------------------------"
echo "[liftr-image-builder] [bake-image.sh | start] Start backing VM image ..."
echo "------------------------------------------------------------------------------------------"
# The following script content is just a sample, it will generate a base VM image for ubuntu dev box.
# You can change the script content as whatever you want.

# echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
# echo "[bake-image.sh] Disabled SBI auto patching for fixed OS, the latest base SBI is chosen at VM image bake time"

# sudo apt-get remove azure-baseimage-patch unattended-upgrades -y
# sudo dpkg --force-all -P azure-baseimage-patch unattended-upgrades
# sudo rm -Rf /var/log/unattended-upgrades

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "[bake-image.sh] Install .NET Core SDK ..."
echo "Source: https://docs.microsoft.com/en-us/dotnet/core/install/linux-package-manager-ubuntu-1804"

echo "[bake-image.sh] Start adding Microsoft repository key and feed ..."
wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo add-apt-repository universe
sudo apt-get update
sudo apt-get install apt-transport-https -y -q
sudo apt-get update

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "[bake-image.sh] Installing the .NET Core SDK 3.1 ..."
sudo apt-get install dotnet-sdk-3.1 -y -q

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "[bake-image.sh] Installing the .NET Core SDK 2.1 ..."
sudo apt-get install dotnet-sdk-2.1 -y -q

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "[bake-image.sh] Installing jq ..."
sudo apt-get install jq -y

echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
echo "[bake-image.sh] Installing zip ..."
sudo apt-get install zip -y

if [[ -z $(command -v az) ]]; then
    echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
    echo "[bake-image.sh] Installing az cli ..."
    curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
else
    echo "az cli already installed."
fi

if [[ -z $(command -v kubectl) ]]; then
    echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
    echo "[bake-image.sh] Installing kubectl ..."
    sudo az aks install-cli
else
    echo "kubectl already installed."
fi

if [[ -z $(command -v docker) ]]; then
  echo "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - "
  echo "[bake-image.sh] Installing docker CE ..."
  sudo apt-get update
  sudo apt-get install -y \
      software-properties-common \
      apt-transport-https \
      ca-certificates \
      curl

  # get-docker.sh using "gpg --dearmor" command, which will not work under Ubuntu 18.04 and many earlier versions.
  # Instead the "apt-key add" has to be used to add needed key
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
  sudo add-apt-repository \
      "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"

  sudo apt-get update
  sudo apt-get install -y --no-install-recommends docker-ce
else
  echo "docker already installed."
fi
echo "docker version:"
sudo docker version

echo "******************************************************************************************"
echo "[liftr-image-builder] [bake-image.sh | end] Finished backing VM image ..."
echo "******************************************************************************************"