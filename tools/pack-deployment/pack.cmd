IF "%CDP_MAJOR_NUMBER_ONLY%"=="" set CDP_MAJOR_NUMBER_ONLY=0
IF "%CDP_MINOR_NUMBER_ONLY%"=="" set CDP_MINOR_NUMBER_ONLY=1
IF "%CDP_BUILD_NUMBER%"=="" set CDP_BUILD_NUMBER=1
IF "%CDP_DEFINITION_BUILD_COUNT%"=="" set CDP_DEFINITION_BUILD_COUNT=2

echo Major version number: %CDP_MAJOR_NUMBER_ONLY%
echo Minor version number: %CDP_MINOR_NUMBER_ONLY%
echo Build number: %CDP_BUILD_NUMBER%
echo CDP_DEFINITION_BUILD_COUNT: %CDP_DEFINITION_BUILD_COUNT%

set NUGET_VERSION=%CDP_MAJOR_NUMBER_ONLY%.%CDP_MINOR_NUMBER_ONLY%.%CDP_BUILD_NUMBER%-build%CDP_DEFINITION_BUILD_COUNT%
echo Nuget version: %NUGET_VERSION%

echo "Remove old contentFiles"
rmdir /s /q contentFiles

echo "Copy deployment folder to contentFiles"
REM https://devblogs.microsoft.com/nuget/nuget-contentFiles-demystified/
xcopy ..\..\deployment contentFiles\any\any\deployment\ /s/h/e/k/f/c

.nuget\nuget.exe pack Liftr.Deployment.nuspec -Version %NUGET_VERSION% -OutputDirectory ..\..\nupkgs