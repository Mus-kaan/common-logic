@echo off

echo "Current path:"
cd

echo "Starting Windows build..."

IF "%CDP_MAJOR_NUMBER_ONLY%"=="" set CDP_MAJOR_NUMBER_ONLY=0
IF "%CDP_MINOR_NUMBER_ONLY%"=="" set CDP_MINOR_NUMBER_ONLY=3
IF "%CDP_BUILD_NUMBER%"=="" set CDP_BUILD_NUMBER=5
IF "%CDP_DEFINITION_BUILD_COUNT%"=="" set CDP_DEFINITION_BUILD_COUNT=20200116

echo Major version number: %CDP_MAJOR_NUMBER_ONLY%
echo Minor version number: %CDP_MINOR_NUMBER_ONLY%
echo Build number: %CDP_BUILD_NUMBER%
echo CDP_DEFINITION_BUILD_COUNT: %CDP_DEFINITION_BUILD_COUNT%

dotnet build %~dp0..\src\Liftr.Common.sln -c Release --no-restore /p:MajorVersion=%CDP_MAJOR_NUMBER_ONLY% /p:MinorVersion=%CDP_MINOR_NUMBER_ONLY% /p:PatchVersion=%CDP_BUILD_NUMBER% /p:BuildMetadata=%CDP_DEFINITION_BUILD_COUNT% || goto :error

echo "Finished Windows build successfully"
goto :EOF

:error
echo Failed with error #%errorlevel%
exit /b %errorlevel%