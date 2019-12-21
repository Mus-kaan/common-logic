@echo off

echo "Packing Windows nugets..."
echo "Current path:"
cd

echo Major version number: %CDP_MAJOR_NUMBER_ONLY%
echo Minor version number: %CDP_MINOR_NUMBER_ONLY%
echo Build number: %CDP_BUILD_NUMBER%
echo CDP_DEFINITION_BUILD_COUNT: %CDP_DEFINITION_BUILD_COUNT%

dotnet pack %~dp0src\Liftr.Common.sln -c Release --include-source --include-symbols --no-build --no-restore -o %~dp0nupkgs /p:MajorVersion=%CDP_MAJOR_NUMBER_ONLY% /p:MinorVersion=%CDP_MINOR_NUMBER_ONLY% /p:PatchVersion=%CDP_BUILD_NUMBER% /p:BuildMetadata=%CDP_DEFINITION_BUILD_COUNT% || goto :error
echo "Finished packing C# nugets successfully"

set NUGET_VERSION=%CDP_MAJOR_NUMBER_ONLY%.%CDP_MINOR_NUMBER_ONLY%.%CDP_BUILD_NUMBER%-build%CDP_DEFINITION_BUILD_COUNT%
echo Nuget version: %NUGET_VERSION%

echo "Remove old contentFiles"
rmdir /s /q %~dp0tools\pack-deployment\contentFiles
rmdir /s /q %~dp0tools\pack-img-builder-files\contentFiles

echo "Copy deployment folder to contentFiles"
REM https://devblogs.microsoft.com/nuget/nuget-contentFiles-demystified/
xcopy %~dp0deployment %~dp0tools\pack-deployment\contentFiles\any\any\deployment\ /s/h/e/k/f/c
xcopy %~dp0image-builder %~dp0tools\pack-img-builder-files\contentFiles\any\any\supporting-files\ /s/h/e/k/f/c

%~dp0tools\.nuget\nuget.exe pack %~dp0tools\pack-deployment\Liftr.Deployment.nuspec -Version %NUGET_VERSION% -OutputDirectory %~dp0nupkgs
%~dp0tools\.nuget\nuget.exe pack %~dp0tools\pack-img-builder-files\Liftr.ImageBuilder.Scripts.nuspec -Version %NUGET_VERSION% -OutputDirectory %~dp0nupkgs

echo "Finished packing nugets successfully"
goto :EOF

:error
echo Failed with error #%errorlevel%
exit /b %errorlevel%