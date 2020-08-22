# Options for mdsd
# Check 'mdsd -h' for details.

MDSD_ROLE_PREFIX=/var/run/mdsd/default
MDSD_OPTIONS="-d -A -r ${MDSD_ROLE_PREFIX}"

# If this is changed, also change /etc/logrotate.d/mdsd
MDSD_LOG=/var/log

# This is where rsyslog and eventhub messages are spooled.
MDSD_SPOOL_DIRECTORY=/var/opt/microsoft/linuxmonagent

MDSD_OPTIONS="-A -c /etc/mdsd.d/mdsd.xml -d -r $MDSD_ROLE_PREFIX -S $MDSD_SPOOL_DIRECTORY/eh -e $MDSD_LOG/mdsd.err -w $MDSD_LOG/mdsd.warn -o $MDSD_LOG/mdsd.info"

export MDSD_TCMALLOC_RELEASE_FREQ_SEC=1
#export MDSD_TCMALLOC_PRINT_STATS_FREQ_SEC=10

export SSL_CERT_DIR=/etc/ssl/certs
#SSL_CERT_FILE

# For instructions on configuring mdsd for GCS, see:
# https://jarvis-west.dc.ad.msft.net/?page=documents&section=9c95f4eb-8689-4c9f-81bf-82d688e860fd&id=69cfaf8a-6417-41b7-a7b4-8d686c4173fe
# In order to enable GCS, uncomment and set all 5 GCS environment variables below

# REQUIRED
# Geneva environment. Examples: Test, FirstPartyProd, DiagnosticsProd
# For the full list of environments, see:
# https://jarvis-west.dc.ad.msft.net/?page=documents&section=1363da01-b6ed-43d4-970e-f89b011d591f&id=d18a0cdb-eb0e-485b-b1bb-cbb6069d352b
#
export MONITORING_GCS_ENVIRONMENT=PLACEHOLDER_MONITORING_GCS_ENVIRONMENT

# REQUIRED
# Geneva Account name
#
export MONITORING_GCS_ACCOUNT=PLACEHOLDER_MONITORING_GCS_ACCOUNT

# REQUIRED
# The region GCS should use when it determines which storage account credentials it should return to MA. e.g. "westus", "eastus".
# Generally, it's best to obtain this value on the VM hosting the agent by querying the Azure Instance Metadata Service (IMDS) for the "location" value (see above code snippet).
#
#export MONITORING_GCS_REGION=westus
# or, pulling data from IMDS
#imdsURL="http://169.254.169.254/metadata/instance/compute/location?api-version=2017-04-02\&format=text"
#export MONITORING_GCS_REGION="$(curl -H Metadata:True --silent $imdsURL)"
export MONITORING_GCS_REGION=PLACEHOLDER_MONITORING_GCS_REGION

# Below are to enable GCS config download. Update for your namespace and config version.
export MONITORING_GCS_NAMESPACE=PLACEHOLDER_MONITORING_GCS_NAMESPACE
export MONITORING_CONFIG_VERSION=PLACEHOLDER_MONITORING_CONFIG_VERSION
export MONITORING_USE_GENEVA_CONFIG_SERVICE=true

export MONITORING_TENANT=PLACEHOLDER_MONITORING_TENANT
export MONITORING_ROLE=PLACEHOLDER_MONITORING_ROLE
export MONITORING_ROLE_INSTANCE=PLACEHOLDER_MONITORING_ROLE_INSTANCE

# The below uses the key vault extension to load the GCS certificate.
#  mdsd will automatically use the new rotated certificate.
# https://genevamondocs.azurewebsites.net/collect/authentication/keyvault.html#use-the-akv-certificate-in-geneva-logs
export MONITORING_GCS_AUTH_ID_TYPE=AuthKeyVault
export MONITORING_GCS_AUTH_ID=PLACEHOLDER_GENEVA_CERT_SAN