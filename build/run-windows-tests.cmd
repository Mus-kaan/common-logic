@echo off

echo "Current path:"
cd

IF [%1] == [] goto start_test
echo "Set LIFTR_UNIT_TEST_AUTH_FILE_BASE64 env variable"
set LIFTR_UNIT_TEST_AUTH_FILE_BASE64=%1
echo "LIFTR_UNIT_TEST_AUTH_FILE_BASE64: %LIFTR_UNIT_TEST_AUTH_FILE_BASE64%"

IF [%2] == [] goto start_test
echo "Set LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64 env variable"
set LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64=%2
echo "LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64: %LIFTR_UNIT_TEST_MONGODB_CONNSTR_BASE64%"

:start_test
echo "Starting Windows tests..."

dotnet test %~dp0..\src\Liftr.Common.sln --logger:trx || goto :error

echo "Finished Windows tests successfully"
goto :EOF

:error
echo Failed with error #%errorlevel%
exit /b %errorlevel%

:end