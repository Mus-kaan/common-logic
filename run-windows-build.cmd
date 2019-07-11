@echo off

echo "Starting Windows build..."

dotnet build %~dp0\src\Liftr.Common.sln -c Release --no-restore || goto :error

echo "Finished Windows build successfully"
goto :EOF

:error
echo Failed with error #%errorlevel%
exit /b %errorlevel%