@echo off

echo "Current path:"
cd

echo "Starting Windows build..."

IF "%CDP_MAJOR_NUMBER_ONLY%"=="" set CDP_MAJOR_NUMBER_ONLY=0
IF "%CDP_MINOR_NUMBER_ONLY%"=="" set CDP_MINOR_NUMBER_ONLY=3
IF "%CDP_BUILD_NUMBER%"=="" set CDP_BUILD_NUMBER=5
IF "%CDP_DEFINITION_BUILD_COUNT%"=="" set CDP_DEFINITION_BUILD_COUNT=1120

echo CDP_MAJOR_NUMBER_ONLY: %CDP_MAJOR_NUMBER_ONLY%
echo CDP_MINOR_NUMBER_ONLY: %CDP_MINOR_NUMBER_ONLY%
echo CDP_DEFINITION_BUILD_COUNT: %CDP_DEFINITION_BUILD_COUNT%
echo CDP_BUILD_NUMBER: %CDP_BUILD_NUMBER%

set NUGET_VERSION=%CDP_MAJOR_NUMBER_ONLY%.%CDP_MINOR_NUMBER_ONLY%.%CDP_DEFINITION_BUILD_COUNT%-x
echo Nuget version: %NUGET_VERSION%

dotnet build %~dp0..\src\Liftr.Common.sln -c Release --no-restore /p:MajorVersion=%CDP_MAJOR_NUMBER_ONLY% /p:MinorVersion=%CDP_MINOR_NUMBER_ONLY% /p:PatchVersion=%CDP_DEFINITION_BUILD_COUNT% /p:BuildMetadata=%CDP_BUILD_NUMBER% -p:PackageVersion=%NUGET_VERSION% || goto :error

echo "Finished Windows build successfully"
goto :EOF

:error
echo Failed with error #%errorlevel%
exit /b %errorlevel%