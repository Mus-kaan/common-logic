@echo off

echo "Current path:"
cd

echo "Starting Windows tests..."

dotnet test %~dp0\src\Liftr.Common.sln --collect:"Code Coverage" --logger:trx || goto :error

echo "Finished Windows tests successfully"
goto :EOF

:error
echo Failed with error #%errorlevel%
exit /b %errorlevel%