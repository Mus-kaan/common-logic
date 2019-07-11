@echo off

echo "Packing Windows nugets..."

dotnet pack %~dp0src\Liftr.Common.sln -c Release --include-source --include-symbols --no-build --no-restore -o %~dp0nupkgs || goto :error

echo "Finished packing nugets successfully"
goto :EOF

:error
echo Failed with error #%errorlevel%
exit /b %errorlevel%