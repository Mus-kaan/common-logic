@echo off

echo "Current path:"
cd

IF [%1] == [] goto end
echo "Set LIFTR_UNIT_TEST_AUTH_FILE_BASE64 env variable"
set LIFTR_UNIT_TEST_AUTH_FILE_BASE64=%1
echo "LIFTR_UNIT_TEST_AUTH_FILE_BASE64: %LIFTR_UNIT_TEST_AUTH_FILE_BASE64%"

IF [%2] == [] goto end
echo "Set LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64 env variable"
set LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64=%2
echo "LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64: %LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64%"

:end