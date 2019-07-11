@echo off

echo "Starting Windows build..."

IF "%CDP_MAJOR_NUMBER_ONLY%"=="" set CDP_MAJOR_NUMBER_ONLY=0
IF "%CDP_MINOR_NUMBER_ONLY%"=="" set CDP_MINOR_NUMBER_ONLY=1
IF "%CDP_BUILD_NUMBER%"=="" set CDP_BUILD_NUMBER=1
IF "%CDP_REVISION_NUMBER%"=="" set CDP_REVISION_NUMBER=2

echo Major version number: %CDP_MAJOR_NUMBER_ONLY%
echo Minor version number: %CDP_MINOR_NUMBER_ONLY%
echo Build number: %CDP_BUILD_NUMBER%
echo Revision number: %CDP_REVISION_NUMBER%

dotnet build %~dp0\src\Liftr.Common.sln -c Release --no-restore /p:MajorVersion=%CDP_MAJOR_NUMBER_ONLY% /p:MinorVersion=%CDP_MINOR_NUMBER_ONLY% /p:PatchVersion=%CDP_BUILD_NUMBER% /p:BuildMetadata=%CDP_REVISION_NUMBER% || goto :error

echo "Finished Windows build successfully"
goto :EOF

:error
echo Failed with error #%errorlevel%
exit /b %errorlevel%