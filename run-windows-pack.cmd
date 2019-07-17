@echo off

echo "Packing Windows nugets..."

echo Major version number: %CDP_MAJOR_NUMBER_ONLY%
echo Minor version number: %CDP_MINOR_NUMBER_ONLY%
echo Build number: %CDP_BUILD_NUMBER%
echo CDP_DEFINITION_BUILD_COUNT: %CDP_DEFINITION_BUILD_COUNT%

dotnet pack %~dp0src\Liftr.Common.sln -c Release --include-source --include-symbols --no-build --no-restore -o %~dp0nupkgs /p:MajorVersion=%CDP_MAJOR_NUMBER_ONLY% /p:MinorVersion=%CDP_MINOR_NUMBER_ONLY% /p:PatchVersion=%CDP_BUILD_NUMBER% /p:BuildMetadata=%CDP_DEFINITION_BUILD_COUNT% || goto :error
dotnet pack %~dp0src\Liftr.Management.sln -c Release --include-source --include-symbols --no-build --no-restore -o %~dp0nupkgs /p:MajorVersion=%CDP_MAJOR_NUMBER_ONLY% /p:MinorVersion=%CDP_MINOR_NUMBER_ONLY% /p:PatchVersion=%CDP_BUILD_NUMBER% /p:BuildMetadata=%CDP_DEFINITION_BUILD_COUNT% || goto :error
dotnet publish %~dp0src\Liftr.Provisioning.Runner\Liftr.Provisioning.Runner.csproj -c Release -f netcoreapp2.2 -o %~dp0exes/Liftr.Provisioning.Runner -r win-x64 --self-contained
echo "Finished packing nugets successfully"
goto :EOF

:error
echo Failed with error #%errorlevel%
exit /b %errorlevel%