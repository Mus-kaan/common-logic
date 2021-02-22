#!/bin/bash

# This is an example script to override APP_ASPNETCORE_ENVIRONMENT
# In Partner Repo if required need to add before_DeployAKSAPP.sh file with content like below to make the change,
# logic mentioned below can be updated as per need basis of specific partner

if [ $REGION = "centralus" ]; then
	APP_ASPNETCORE_ENVIRONMENT=Test
fi